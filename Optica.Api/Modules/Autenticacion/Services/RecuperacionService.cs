using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class RecuperacionService(
    OpticaDbContext db,
    EstadoRecuperacion estado,
    IPasswordHasher<CuentaCliente> clienteHasher,
    IPasswordHasher<Trabajador> personalHasher,
    IEmailService email,
    TimeProvider timeProvider,
    ILogger<RecuperacionService> logger)
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(5);
    private const int IntentosMaximos = 5;

    public async Task SolicitarAsync(string identificador, CancellationToken ct)
    {
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        estado.LimpiarExpirados(ahora);
        var resolucion = await ResolverAsync(identificador, ct);
        if (resolucion is null || !resolucion.Activa) return;

        var clave = ClaveCuenta(resolucion);
        var bloqueo = estado.ObtenerBloqueo(clave);
        await bloqueo.WaitAsync(ct);
        try
        {
            // Cada solicitud nueva reemplaza el código anterior de esa cuenta.
            estado.Invalidar(clave);
            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var cuerpo = $"<p>Tu código para recuperar la contraseña es:</p><p style=\"font-size:30px;font-weight:bold\">{HtmlEncoder.Default.Encode(codigo)}</p><p>Vence en 5 minutos y solo puede utilizarse una vez.</p>";
            var resultado = await email.EnviarConResultadoAsync(resolucion.Correo,
                "Recuperación de contraseña - Centro Óptico San Francisco", cuerpo, ct, importante: true);
            if (resultado.Exitoso)
                estado.Guardar(clave, new EstadoRecuperacion.Desafio(codigo, ahora.Add(Vigencia), 0));
            else
                logger.LogWarning("No se pudo enviar un código de recuperación ({TipoCuenta}).", resolucion.Tipo);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se pudo enviar un código de recuperación ({TipoCuenta}, {TipoError}).", resolucion.Tipo, ex.GetType().Name);
        }
        finally
        {
            bloqueo.Release();
        }
    }

    public async Task<bool> ConfirmarAsync(string identificador, string codigo, string nuevaContrasena,
        string confirmarContrasena, CancellationToken ct)
    {
        if (!string.Equals(nuevaContrasena, confirmarContrasena, StringComparison.Ordinal))
            throw new ArgumentException("La nueva contraseña y su confirmación no coinciden.");
        if (!AutenticacionClienteService.ContrasenaValida(nuevaContrasena))
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.");
        if (codigo.Length != 6 || codigo.Any(c => !char.IsAsciiDigit(c))) return false;

        var resolucion = await ResolverAsync(identificador, ct);
        if (resolucion is null || !resolucion.Activa) return false;

        var clave = ClaveCuenta(resolucion);
        var bloqueo = estado.ObtenerBloqueo(clave);
        await bloqueo.WaitAsync(ct);
        try
        {
            var desafio = estado.Obtener(clave);
            if (desafio is null) return false;
            if (desafio.ExpiraUtc <= timeProvider.GetUtcNow().UtcDateTime)
            {
                estado.Invalidar(clave);
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(desafio.Codigo), Encoding.UTF8.GetBytes(codigo)))
            {
                var intentos = desafio.Intentos + 1;
                if (intentos >= IntentosMaximos) estado.Invalidar(clave);
                else estado.Guardar(clave, desafio with { Intentos = intentos });
                return false;
            }

            // El código es de un solo uso y se conserva solo en memoria.
            estado.Invalidar(clave);
            if (resolucion.Tipo == "Cliente")
            {
                var cuenta = await db.CuentasCliente.Include(c => c.Cliente)
                    .SingleOrDefaultAsync(c => c.IdCuenta == resolucion.Id, ct);
                if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo") return false;
                cuenta.ContrasenaHash = clienteHasher.HashPassword(cuenta, nuevaContrasena);
            }
            else
            {
                var personal = await db.Trabajadores.SingleOrDefaultAsync(p =>
                    p.IdTrabajador == resolucion.Id && p.Estado == "Activo", ct);
                if (personal is null || (resolucion.Tipo == "Administrador") !=
                    (personal is Optica.Api.Modules.AgendaReservas.Models.Administrador)) return false;
                personal.Contrasena = personalHasher.HashPassword(personal, nuevaContrasena);
            }

            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            bloqueo.Release();
        }
    }

    private async Task<CuentaResuelta?> ResolverAsync(string identificador, CancellationToken ct)
    {
        var valor = identificador.Trim();
        if (RutChilenoValidator.EsValido(valor))
        {
            var rut = RutChilenoValidator.Normalizar(valor);
            var cuenta = await db.CuentasCliente.Include(c => c.Cliente).SingleOrDefaultAsync(c =>
                c.Cliente != null && c.Cliente.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut, ct);
            return cuenta is null ? null : new("Cliente", cuenta.IdCuenta, cuenta.Correo,
                cuenta.Estado == "Activa" && cuenta.Cliente?.Estado == "Activo");
        }

        var personal = await db.Trabajadores.SingleOrDefaultAsync(p =>
            p.NombreUsuario.ToUpper() == valor.ToUpper(), ct);
        return personal is null ? null : new(
            personal is Optica.Api.Modules.AgendaReservas.Models.Administrador ? "Administrador" : "Trabajador",
            personal.IdTrabajador, personal.Correo, personal.Estado == "Activo");
    }

    private static string ClaveCuenta(CuentaResuelta cuenta) => $"{cuenta.Tipo}:{cuenta.Id}";

    private sealed record CuentaResuelta(string Tipo, int Id, string Correo, bool Activa);
}
