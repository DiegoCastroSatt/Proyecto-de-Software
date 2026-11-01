using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class RecuperacionService(
    OpticaDbContext db,
    IPasswordHasher<CuentaCliente> clienteHasher,
    IPasswordHasher<Trabajador> personalHasher,
    IEmailService email,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<RecuperacionService> logger)
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(5);

    public async Task<bool> SolicitarAsync(string identificador, string? ip, CancellationToken ct)
    {
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var pepper = ObtenerPepper();
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM limites_recuperacion WHERE inicio_ventana_utc <= {ahora.AddDays(-1)}", ct);
        var resolucion = await ResolverAsync(identificador, ct);
        var huellaIdentificador = Huella(pepper, "identificador:" + Normalizar(identificador));
        var huellaIp = Huella(pepper, "ip:" + (string.IsNullOrWhiteSpace(ip) ? "desconocida" : ip.Trim()));
        var huellaCuenta = Huella(pepper, "cuenta:" + (resolucion is null
            ? Normalizar(identificador)
            : $"{resolucion.Tipo}:{resolucion.Id}"));

        if (!await RegistrarLimiteAsync(huellaCuenta, ahora, TimeSpan.FromHours(1), 5, ct)
            || !await RegistrarLimiteAsync(huellaIp, ahora, TimeSpan.FromMinutes(15), 10, ct))
            return false;

        if (resolucion is null || !resolucion.Activa)
            return true;

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        DesafioRecuperacion desafio;
        try
        {
            if (resolucion.Tipo == "Cliente")
                _ = await db.CuentasCliente.FromSqlInterpolated($"SELECT * FROM cuentas_cliente WHERE id_cuenta = {resolucion.Id} FOR UPDATE").ToListAsync(ct);
            else
                _ = await db.Trabajadores.FromSqlInterpolated($"SELECT * FROM trabajador_optica WHERE id_trabajador = {resolucion.Id} FOR UPDATE").ToListAsync(ct);

            var anteriores = await db.DesafiosRecuperacion.Where(d => d.Estado == "Pendiente"
                && (resolucion.Tipo == "Cliente" ? d.IdCuentaCliente == resolucion.Id : d.IdPersonal == resolucion.Id)).ToListAsync(ct);
            foreach (var anterior in anteriores) anterior.Estado = "Invalidado";

            desafio = new DesafioRecuperacion
            {
                TipoCuenta = resolucion.Tipo,
                IdCuentaCliente = resolucion.Tipo == "Cliente" ? resolucion.Id : null,
                IdPersonal = resolucion.Tipo == "Cliente" ? null : resolucion.Id,
                HashCodigo = HashCodigo(pepper, resolucion.Tipo, resolucion.Id, codigo),
                CreadoUtc = ahora,
                ExpiraUtc = ahora.Add(Vigencia),
                Estado = "Pendiente",
                HuellaIdentificador = huellaIdentificador,
                HuellaIp = huellaIp
            };
            db.DesafiosRecuperacion.Add(desafio);
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }
        catch
        {
            await transaccion.RollbackAsync(ct);
            throw;
        }

        var cuerpo = $"<p>Tu código para recuperar la contraseña es:</p><p style=\"font-size:30px;font-weight:bold\">{HtmlEncoder.Default.Encode(codigo)}</p><p>Vence en 5 minutos y solo puede utilizarse una vez.</p>";
        try
        {
            var enviado = await email.EnviarConResultadoAsync(resolucion.Correo,
                "Recuperación de contraseña - Centro Óptico San Francisco", cuerpo, ct, importante: true);
            if (!enviado.Exitoso)
            {
                desafio.Estado = "Invalidado";
                await db.SaveChangesAsync(ct);
                logger.LogWarning("No se pudo enviar un código de recuperación ({TipoCuenta}).", resolucion.Tipo);
            }
        }
        catch (Exception ex)
        {
            desafio.Estado = "Invalidado";
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogWarning("No se pudo enviar un código de recuperación ({TipoCuenta}, {TipoError}).", resolucion.Tipo, ex.GetType().Name);
            if (ex is OperationCanceledException && ct.IsCancellationRequested) throw;
        }
        return true;
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
        var pepper = ObtenerPepper();
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var desafio = resolucion.Tipo == "Cliente"
                ? (await db.DesafiosRecuperacion.FromSqlInterpolated($"SELECT * FROM desafios_recuperacion WHERE tipo_cuenta = 'Cliente' AND id_cuenta_cliente = {resolucion.Id} AND estado = 'Pendiente' FOR UPDATE").ToListAsync(ct)).SingleOrDefault()
                : (await db.DesafiosRecuperacion.FromSqlInterpolated($"SELECT * FROM desafios_recuperacion WHERE tipo_cuenta = {resolucion.Tipo} AND id_personal = {resolucion.Id} AND estado = 'Pendiente' FOR UPDATE").ToListAsync(ct)).SingleOrDefault();
            if (desafio is null) return false;
            var ahora = timeProvider.GetUtcNow().UtcDateTime;
            if (desafio.ExpiraUtc <= ahora)
            {
                desafio.Estado = "Expirado";
                await db.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
                return false;
            }

            var esperado = Convert.FromHexString(desafio.HashCodigo);
            var recibido = Convert.FromHexString(HashCodigo(pepper, resolucion.Tipo, resolucion.Id, codigo));
            if (!CryptographicOperations.FixedTimeEquals(esperado, recibido))
            {
                desafio.Intentos++;
                if (desafio.Intentos >= 5) desafio.Estado = "Agotado";
                await db.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
                return false;
            }

            if (resolucion.Tipo == "Cliente")
            {
                var cuenta = await db.CuentasCliente.Include(c => c.Cliente).SingleOrDefaultAsync(c => c.IdCuenta == resolucion.Id, ct);
                if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo")
                {
                    desafio.Estado = "Invalidado";
                    await db.SaveChangesAsync(ct);
                    await transaccion.CommitAsync(ct);
                    return false;
                }
                cuenta.ContrasenaHash = clienteHasher.HashPassword(cuenta, nuevaContrasena);
                await db.DesafiosRecuperacion.Where(d => d.IdCuentaCliente == cuenta.IdCuenta && d.Estado == "Pendiente")
                    .ExecuteUpdateAsync(u => u.SetProperty(d => d.Estado, "Invalidado"), ct);
            }
            else
            {
                var personal = await db.Trabajadores.SingleOrDefaultAsync(p => p.IdTrabajador == resolucion.Id && p.Estado == "Activo", ct);
                if (personal is null || (resolucion.Tipo == "Administrador") != (personal is Optica.Api.Modules.AgendaReservas.Models.Administrador))
                {
                    desafio.Estado = "Invalidado";
                    await db.SaveChangesAsync(ct);
                    await transaccion.CommitAsync(ct);
                    return false;
                }
                personal.Contrasena = personalHasher.HashPassword(personal, nuevaContrasena);
                await db.DesafiosRecuperacion.Where(d => d.IdPersonal == personal.IdTrabajador && d.Estado == "Pendiente")
                    .ExecuteUpdateAsync(u => u.SetProperty(d => d.Estado, "Invalidado"), ct);
            }
            desafio.Estado = "Usado";
            await db.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaccion.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<bool> RegistrarLimiteAsync(string huella, DateTime ahora, TimeSpan ventana, int maximo, CancellationToken ct)
    {
        var inicio = ahora.Subtract(ventana);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO limites_recuperacion (huella, inicio_ventana_utc, solicitudes, ultima_solicitud_utc) VALUES ({huella}, {ahora}, 1, {ahora}) ON DUPLICATE KEY UPDATE solicitudes = IF(inicio_ventana_utc <= {inicio}, 1, LEAST(solicitudes + 1, 1000000)), inicio_ventana_utc = IF(inicio_ventana_utc <= {inicio}, {ahora}, inicio_ventana_utc), ultima_solicitud_utc = {ahora}", ct);
        var limite = await db.LimitesRecuperacion.AsNoTracking().SingleAsync(l => l.Huella == huella, ct);
        return limite.Solicitudes <= maximo;
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
        var personal = await db.Trabajadores.SingleOrDefaultAsync(p => p.NombreUsuario.ToUpper() == valor.ToUpper(), ct);
        return personal is null ? null : new(personal is Optica.Api.Modules.AgendaReservas.Models.Administrador ? "Administrador" : "Trabajador",
            personal.IdTrabajador, personal.Correo, personal.Estado == "Activo");
    }

    private string ObtenerPepper()
    {
        var pepper = configuration["Authentication:RecoveryCodePepper"];
        if (string.IsNullOrWhiteSpace(pepper) || Encoding.UTF8.GetByteCount(pepper) < 32)
            throw new InvalidOperationException("Debe configurarse un secreto de recuperación de al menos 32 bytes mediante Authentication__RecoveryCodePepper.");
        return pepper;
    }

    private static string Normalizar(string value) => value.Trim().Replace(".", "", StringComparison.Ordinal)
        .Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
    private static string Huella(string pepper, string value) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes(value)));
    private static string HashCodigo(string pepper, string tipo, int id, string codigo) =>
        Huella(pepper, $"codigo:{tipo}:{id}:{codigo}");

    private sealed record CuentaResuelta(string Tipo, int Id, string Correo, bool Activa);
}
