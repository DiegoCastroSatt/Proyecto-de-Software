using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Optica.Api.Data;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class AutenticacionClienteService(
    OpticaDbContext db,
    IPasswordHasher<CuentaCliente> passwordHasher,
    EstadoRecuperacionCliente estadoRecuperacion,
    IEmailService emailService,
    TimeProvider proveedorTiempo,
    ILogger<AutenticacionClienteService> logger,
    IConfiguration configuracion)
{
    private const string NombreCentro = "Centro Óptico San Francisco";
    private static readonly TimeSpan VigenciaConfirmacion = TimeSpan.FromHours(24);

    public async Task<(ClienteSesionResponse? Sesion, bool CredencialesValidas)> IniciarSesionAsync(
        LoginClienteRequest request, CancellationToken cancellationToken)
    {
        var correo = NormalizarCorreo(request.Correo);
        var cuenta = await db.CuentasCliente
            .Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.CorreoNormalizado == correo, cancellationToken);

        if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo")
            return (null, false);

        var resultado = passwordHasher.VerifyHashedPassword(cuenta, cuenta.ContrasenaHash, request.Contrasena);
        if (resultado == PasswordVerificationResult.Failed)
            return (null, false);

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, request.Contrasena);
            await db.SaveChangesAsync(cancellationToken);
        }

        return (CrearSesion(cuenta), true);
    }

    public async Task<ResultadoRegistroCliente> RegistrarAsync(
        RegistroClienteRequest request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre.Trim();
        var apellido = request.Apellido.Trim();
        var rut = RutChilenoValidator.Normalizar(request.Rut);
        var correo = request.Correo.Trim();
        var correoNormalizado = NormalizarCorreo(correo);
        var telefono = request.Telefono.Trim();

        if (nombre.Length == 0 || apellido.Length == 0)
            return ResultadoRegistroCliente.Invalido("Ingresa nombre y apellido.");
        if (!RutChilenoValidator.EsValido(request.Rut))
            return ResultadoRegistroCliente.Invalido("El RUT ingresado no es válido.");
        if (correo.Length > 100 || !MailAddress.TryCreate(correo, out var direccion)
            || !string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase))
            return ResultadoRegistroCliente.Invalido("Ingresa un correo electrónico válido de hasta 100 caracteres.");
        if (!TelefonoChilenoValidator.EsValido(telefono))
            return ResultadoRegistroCliente.Invalido("Ingresa un celular válido de 9 dígitos; puedes incluir el prefijo +56.");
        if (!ContrasenaValida(request.Contrasena))
            return ResultadoRegistroCliente.Invalido("La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.");
        if (!string.Equals(request.Contrasena, request.ConfirmarContrasena, StringComparison.Ordinal))
            return ResultadoRegistroCliente.Invalido("Las contraseñas no coinciden.");

        var rutExistente = await db.Clientes.AnyAsync(c =>
            c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut, cancellationToken);
        var correoExistente = await db.Clientes.AnyAsync(c => c.Correo != null
            && c.Correo.Trim().ToUpper() == correoNormalizado, cancellationToken);
        var cuentaExistente = await db.CuentasCliente.AnyAsync(c =>
            c.CorreoNormalizado == correoNormalizado, cancellationToken);
        if (rutExistente || correoExistente || cuentaExistente)
            return ResultadoRegistroCliente.Conflicto();

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var cliente = new Cliente
            {
                Rut = rut,
                Nombre = nombre,
                Apellido = apellido,
                Correo = correo,
                Telefono = telefono,
                Estado = "Activo",
                FechaRegistro = DateTime.Now
            };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(cancellationToken);

            var cuenta = new CuentaCliente
            {
                IdCliente = cliente.IdCliente,
                Correo = correo,
                CorreoNormalizado = correoNormalizado,
                Estado = "PendienteConfirmacion",
                FechaCreacion = DateTime.UtcNow
            };
            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, request.Contrasena);
            var token = CrearTokenConfirmacion();
            cuenta.TokenConfirmacionHash = HashToken(token);
            cuenta.TokenConfirmacionExpiraUtc = DateTime.UtcNow.Add(VigenciaConfirmacion);
            cuenta.UltimoCorreoConfirmacionUtc = DateTime.UtcNow;
            db.CuentasCliente.Add(cuenta);
            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            var correoEnviado = await EnviarCorreoConfirmacionAsync(cuenta, cliente.Nombre, token, cancellationToken);
            if (!correoEnviado)
                logger.LogWarning("La cuenta de cliente {ClienteId} quedó pendiente de confirmación; no se pudo enviar el correo.", cliente.IdCliente);
            return ResultadoRegistroCliente.Exitoso(correoEnviado);
        }
        catch (DbUpdateException ex) when (EsDuplicadoMySql(ex))
        {
            await transaccion.RollbackAsync(cancellationToken);
            return ResultadoRegistroCliente.Conflicto();
        }
    }

    public async Task<bool> ConfirmarCuentaAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var hashToken = HashToken(token);
        var ahora = proveedorTiempo.GetUtcNow().UtcDateTime;
        if (!db.Database.IsRelational())
        {
            var cuentaEnMemoria = await db.CuentasCliente.SingleOrDefaultAsync(c =>
                c.TokenConfirmacionHash == hashToken && c.Estado == "PendienteConfirmacion"
                    && c.TokenConfirmacionExpiraUtc > ahora, cancellationToken);
            if (cuentaEnMemoria is null) return false;
            cuentaEnMemoria.Estado = "Activa";
            cuentaEnMemoria.TokenConfirmacionHash = null;
            cuentaEnMemoria.TokenConfirmacionExpiraUtc = null;
            cuentaEnMemoria.UltimoCorreoConfirmacionUtc = null;
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        var actualizadas = await db.CuentasCliente
            .Where(c => c.TokenConfirmacionHash == hashToken
                && c.Estado == "PendienteConfirmacion"
                && c.TokenConfirmacionExpiraUtc > ahora)
            .ExecuteUpdateAsync(update => update
                .SetProperty(c => c.Estado, "Activa")
                .SetProperty(c => c.TokenConfirmacionHash, (string?)null)
                .SetProperty(c => c.TokenConfirmacionExpiraUtc, (DateTime?)null)
                .SetProperty(c => c.UltimoCorreoConfirmacionUtc, (DateTime?)null), cancellationToken);
        return actualizadas == 1;
    }

    public async Task ReenviarConfirmacionAsync(string correoIngresado, CancellationToken cancellationToken)
    {
        var correoNormalizado = NormalizarCorreo(correoIngresado);
        var cuenta = await db.CuentasCliente.Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.CorreoNormalizado == correoNormalizado
                && c.Estado == "PendienteConfirmacion", cancellationToken);
        var ahora = proveedorTiempo.GetUtcNow().UtcDateTime;
        if (cuenta?.Cliente is null || (cuenta.UltimoCorreoConfirmacionUtc is { } ultimo
            && ahora - ultimo < TimeSpan.FromMinutes(1))) return;

        var token = CrearTokenConfirmacion();
        cuenta.TokenConfirmacionHash = HashToken(token);
        cuenta.TokenConfirmacionExpiraUtc = ahora.Add(VigenciaConfirmacion);
        cuenta.UltimoCorreoConfirmacionUtc = ahora;
        await db.SaveChangesAsync(cancellationToken);
        var enviado = await EnviarCorreoConfirmacionAsync(cuenta, cuenta.Cliente.Nombre, token, cancellationToken);
        if (!enviado)
            logger.LogWarning("No se pudo reenviar la confirmación de la cuenta de cliente {ClienteId}.", cuenta.IdCliente);
    }

    private async Task<bool> EnviarCorreoConfirmacionAsync(
        CuentaCliente cuenta, string nombre, string token, CancellationToken cancellationToken)
    {
        var baseFrontend = (configuracion["Correo:UrlBaseFrontend"] ?? "http://localhost:4200").TrimEnd('/');
        var enlace = $"{baseFrontend}/cliente/confirmar-cuenta?token={Uri.EscapeDataString(token)}";
        var enlaceHtml = HtmlEncoder.Default.Encode(enlace);
        var nombreHtml = HtmlEncoder.Default.Encode(nombre);
        var cuerpoHtml = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Confirma tu correo electrónico</h1>
            <p>Hola {nombreHtml}, para activar tu cuenta confirma que tienes acceso a este correo.</p>
            <p style="margin:24px 0"><a href="{enlaceHtml}" style="background:#176b6c;color:#fff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:bold;display:inline-block">Confirmar mi cuenta</a></p>
            <p>Este enlace vence en 24 horas y solo puede utilizarse una vez.</p>
            <p>Si no creaste esta cuenta, puedes ignorar este mensaje.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;
        var resultado = await emailService.EnviarConResultadoAsync(
            cuenta.Correo, $"Confirma tu cuenta - {NombreCentro}", cuerpoHtml, cancellationToken, importante: true);
        return resultado.Exitoso;
    }

    private static string CrearTokenConfirmacion() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();

    public async Task<bool> SolicitarRecuperacionAsync(
        string correoIngresado,
        string? ipCliente,
        CancellationToken cancellationToken)
    {
        var correo = NormalizarCorreo(correoIngresado);
        var ahora = proveedorTiempo.GetUtcNow();

        if (!estadoRecuperacion.IntentarIniciarSolicitud(correo, ipCliente, ahora))
            return false;

        var cuenta = await db.CuentasCliente
            .Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.CorreoNormalizado == correo, cancellationToken);

        // Si la cuenta no existe o está inactiva, retornamos true para evitar enumeración de usuarios
        if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo")
            return true;

        var token = estadoRecuperacion.GenerarToken();
        var baseFrontend = (configuracion["Correo:UrlBaseFrontend"] ?? "http://localhost:4200").TrimEnd('/');
        var enlace = $"{baseFrontend}/cliente/restablecer-contrasena?token={Uri.EscapeDataString(token)}";

        var asunto = $"Recuperación de contraseña - {NombreCentro}";
        var enlaceHtml = HtmlEncoder.Default.Encode(enlace);
        var nombreDestinatario = HtmlEncoder.Default.Encode(cuenta.Cliente.Nombre);

        var cuerpoHtml = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Recuperación de contraseña</h1>
            <p>Hola {nombreDestinatario}, recibimos una solicitud para restablecer la contraseña de tu cuenta de cliente.</p>
            <p style="margin:24px 0">
                <a href="{enlaceHtml}" style="background:#176b6c;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:bold;display:inline-block">
                    Restablecer contraseña
                </a>
            </p>
            <p>Este enlace vence en 15 minutos y solo puede utilizarse una vez.</p>
            <p>Si tú no solicitaste este cambio, puedes ignorar este mensaje; tu contraseña seguirá siendo la misma.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;

        var resultado = await emailService.EnviarConResultadoAsync(
            cuenta.Correo, asunto, cuerpoHtml, cancellationToken, importante: true);

        if (!resultado.Exitoso)
        {
            logger.LogWarning("No se pudo enviar el correo de recuperación al cliente {ClienteId}.", cuenta.IdCliente);
            return true;
        }

        estadoRecuperacion.GuardarSolicitud(token, cuenta.IdCliente, cuenta.Correo, ahora);
        return true;
    }

    public async Task<bool> ConfirmarRecuperacionAsync(
        string token,
        string nuevaContrasena,
        string confirmarContrasena,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(nuevaContrasena, confirmarContrasena, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas ingresadas no coinciden.");

        if (!ContrasenaValida(nuevaContrasena))
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo.");

        var validacion = estadoRecuperacion.ValidarToken(token, proveedorTiempo.GetUtcNow());
        if (!validacion.Valido)
            return false;

        try
        {
            var cuenta = await db.CuentasCliente
                .Include(c => c.Cliente)
                .SingleOrDefaultAsync(c => c.IdCliente == validacion.IdCliente, cancellationToken);

            if (cuenta is null || cuenta.Estado != "Activa" || cuenta.Cliente?.Estado != "Activo")
            {
                estadoRecuperacion.FinalizarCambio(token, exitoso: true);
                return false;
            }

            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, nuevaContrasena);
            await db.SaveChangesAsync(cancellationToken);

            estadoRecuperacion.FinalizarCambio(token, exitoso: true);
            return true;
        }
        catch
        {
            estadoRecuperacion.FinalizarCambio(token, exitoso: false);
            throw;
        }
    }

    public static ClienteSesionResponse CrearSesion(CuentaCliente cuenta) => new(
        cuenta.IdCliente,
        $"{cuenta.Cliente?.Nombre} {cuenta.Cliente?.Apellido}".Trim(),
        cuenta.Correo);

    public static string NormalizarCorreo(string correo) => correo.Trim().ToUpperInvariant();

    public static bool ContrasenaValida(string contrasena) =>
        contrasena.Length is >= 8 and <= 255
        && contrasena.Any(char.IsLetter)
        && contrasena.Any(char.IsDigit)
        && contrasena.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

    private static bool EsDuplicadoMySql(Exception exception)
    {
        for (Exception? actual = exception; actual is not null; actual = actual.InnerException)
            if (actual is MySqlException mysql && mysql.Number == 1062)
                return true;
        return false;
    }
}

public sealed record ResultadoRegistroCliente(bool Registrado, string? Error, bool ConflictoExistente, bool CorreoConfirmacionEnviado)
{
    public static ResultadoRegistroCliente Exitoso(bool correoEnviado) => new(true, null, false, correoEnviado);
    public static ResultadoRegistroCliente Invalido(string error) => new(false, error, false, false);
    public static ResultadoRegistroCliente Conflicto() => new(false, null, true, false);
}
