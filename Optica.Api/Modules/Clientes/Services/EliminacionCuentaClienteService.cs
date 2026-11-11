using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Services;

// Elimina la cuenta web de un cliente y anonimiza su ficha sin romper la integridad referencial.
// Se conservan ventas, pedidos, recetas y reseñas (obligaciones comerciales/tributarias),
// pero sin datos que permitan identificar a la persona.
public sealed class EliminacionCuentaClienteService(
    OpticaDbContext db,
    IPasswordHasher<CuentaCliente> passwordHasher,
    IWebHostEnvironment entorno,
    TimeProvider proveedorTiempo,
    ILogger<EliminacionCuentaClienteService> logger)
{
    public const string PrefijoRutAnonimo = "ANON-";
    public const string NombreAnonimo = "Cliente";
    public const string ApellidoAnonimo = "Eliminado";

    // Estados de pedido que impiden eliminar la cuenta hasta que se entreguen o cancelen.
    public static readonly string[] EstadosPedidoEnCurso = ["Pendiente", "En proceso", "Listo"];
    private static readonly string[] EstadosReservaActiva = ["Pendiente", "Confirmada"];

    public static string CrearRutAnonimo(int idCliente) => $"{PrefijoRutAnonimo}{idCliente}";

    public static bool EsRutAnonimo(string rut) => rut.StartsWith(PrefijoRutAnonimo, StringComparison.Ordinal);

    public async Task<ResultadoEliminacionCuenta> EliminarAsync(
        int idCliente, string contrasenaActual, CancellationToken cancellationToken)
    {
        var cuenta = await db.CuentasCliente.Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.IdCliente == idCliente && c.Estado == "Activa"
                && c.Cliente != null && c.Cliente.Estado == "Activo", cancellationToken);
        if (cuenta?.Cliente is null) return ResultadoEliminacionCuenta.SinCuenta;

        // Confirmación de identidad: se exige la contraseña actual.
        if (string.IsNullOrWhiteSpace(contrasenaActual)
            || passwordHasher.VerifyHashedPassword(cuenta, cuenta.ContrasenaHash, contrasenaActual) == PasswordVerificationResult.Failed)
            return ResultadoEliminacionCuenta.ContrasenaIncorrecta;

        var cliente = cuenta.Cliente;
        var rutOriginal = cliente.Rut;
        if (await db.Pedidos.AnyAsync(p => p.Rut == rutOriginal && EstadosPedidoEnCurso.Contains(p.Estado), cancellationToken))
            return ResultadoEliminacionCuenta.PedidosEnCurso;

        var rutAnonimo = CrearRutAnonimo(cliente.IdCliente);
        var hoy = proveedorTiempo.GetLocalNow().Date;
        var imagenesPorBorrar = new List<string>();

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        // 1. Borrar el acceso web (login, hash y tokens).
        db.CuentasCliente.Remove(cuenta);

        // 2. Anonimizar la ficha. En MySQL, ON UPDATE CASCADE propaga el nuevo RUT a pedidos.
        cliente.Nombre = NombreAnonimo;
        cliente.Apellido = ApellidoAnonimo;
        cliente.Rut = rutAnonimo;
        cliente.Telefono = null;
        cliente.Correo = null;
        cliente.Estado = "Inactivo";
        await db.SaveChangesAsync(cancellationToken);

        // Pedidos: se conservan montos, fechas y estados; se limpia el texto libre.
        // Se asigna el RUT explícitamente para proveedores sin cascada (por ejemplo, en memoria).
        var pedidos = await db.Pedidos
            .Where(p => p.Rut == rutOriginal || p.Rut == rutAnonimo)
            .ToListAsync(cancellationToken);
        foreach (var pedido in pedidos)
        {
            pedido.Rut = rutAnonimo;
            pedido.Anotaciones = null;
        }

        // Reservas: se limpia el motivo y los tokens; las futuras activas se cancelan.
        var reservas = await db.Reservas.Where(r => r.ClienteId == idCliente).ToListAsync(cancellationToken);
        var idsHorario = reservas.Select(r => r.HorarioId).Distinct().ToList();
        var fechasHorario = await db.Horarios.Where(h => idsHorario.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Fecha, cancellationToken);
        foreach (var reserva in reservas)
        {
            reserva.Motivo = null;
            reserva.TokenConfirmacionHash = null;
            reserva.TokenCancelacionHash = null;
            reserva.TokenAccionExpiraUtc = null;
            if (EstadosReservaActiva.Contains(reserva.Estado)
                && fechasHorario.TryGetValue(reserva.HorarioId, out var fecha) && fecha.Date >= hoy)
                reserva.Estado = "Cancelada";
        }

        // 3. Eliminar los correos guardados (destinatario y contenido con datos personales).
        var idsPedido = pedidos.Select(p => p.IdPedido).ToList();
        var idsReserva = reservas.Select(r => r.Id).ToList();
        db.CorreosPedido.RemoveRange(await db.CorreosPedido.Include(c => c.Historial)
            .Where(c => idsPedido.Contains(c.PedidoId)).ToListAsync(cancellationToken));
        db.CorreosReserva.RemoveRange(await db.CorreosReserva.Include(c => c.Historial)
            .Where(c => idsReserva.Contains(c.ReservaId)).ToListAsync(cancellationToken));

        // Encuestas: se anulan los enlaces pendientes. Las respuestas ya son anónimas y se conservan.
        var encuestasPendientes = await db.EncuestasAtencion
            .Where(e => idsReserva.Contains(e.ReservaId) && !e.Respondida && e.TokenHash != null)
            .ToListAsync(cancellationToken);
        foreach (var encuesta in encuestasPendientes)
        {
            encuesta.TokenHash = null;
            encuesta.TokenExpiraUtc = null;
        }

        // Recetas y graduaciones: se conservan; se quitan las imágenes subidas.
        var recetas = await db.Recetas.Where(r => r.ClienteId == idCliente && r.ImagenPath != null)
            .ToListAsync(cancellationToken);
        foreach (var receta in recetas)
        {
            imagenesPorBorrar.Add(receta.ImagenPath!);
            receta.ImagenPath = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        // Los archivos se borran después del commit: si la transacción falla, las imágenes siguen disponibles.
        foreach (var ruta in imagenesPorBorrar)
            BorrarImagenReceta(ruta);

        logger.LogInformation("Se eliminó la cuenta del cliente {ClienteId} y se anonimizó su ficha.", idCliente);
        return ResultadoEliminacionCuenta.Eliminada;
    }

    private void BorrarImagenReceta(string rutaRelativa)
    {
        try
        {
            var raizWeb = entorno.WebRootPath ?? Path.Combine(entorno.ContentRootPath, "wwwroot");
            var carpetaRecetas = Path.GetFullPath(Path.Combine(raizWeb, "uploads", "recetas")) + Path.DirectorySeparatorChar;
            var rutaCompleta = Path.GetFullPath(Path.Combine(raizWeb, rutaRelativa.TrimStart('/', '\\')));
            // Solo se borran archivos dentro de uploads/recetas.
            if (!rutaCompleta.StartsWith(carpetaRecetas, StringComparison.OrdinalIgnoreCase)) return;
            if (File.Exists(rutaCompleta)) File.Delete(rutaCompleta);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("No se pudo borrar una imagen de receta de un cliente eliminado ({TipoError}).", ex.GetType().Name);
        }
    }
}

public enum ResultadoEliminacionCuenta
{
    Eliminada, SinCuenta, ContrasenaIncorrecta, PedidosEnCurso
}
