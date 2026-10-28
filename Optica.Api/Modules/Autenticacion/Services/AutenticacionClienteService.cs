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
    ILogger<AutenticacionClienteService> logger)
{
    private const string NombreCentro = "Centro Óptico San Francisco";
    private const string UrlBaseFrontend = "http://localhost:4200";

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
                Estado = "Activa",
                FechaCreacion = DateTime.UtcNow
            };
            cuenta.ContrasenaHash = passwordHasher.HashPassword(cuenta, request.Contrasena);
            db.CuentasCliente.Add(cuenta);
            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return ResultadoRegistroCliente.Exitoso();
        }
        catch (DbUpdateException ex) when (EsDuplicadoMySql(ex))
        {
            await transaccion.RollbackAsync(cancellationToken);
            return ResultadoRegistroCliente.Conflicto();
        }
    }

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
        var enlace = $"{UrlBaseFrontend}/cliente/restablecer-contrasena?token={Uri.EscapeDataString(token)}";

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
            <p>Si el botón no funciona, copia y pega el siguiente enlace en tu navegador:</p>
            <p style="word-break:break-all;color:#176b6c;font-size:13px">{enlaceHtml}</p>
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
        cuenta.Correo,
        cuenta.Cliente?.Rut ?? string.Empty);

    public static string NormalizarCorreo(string correo) => correo.Trim().ToUpperInvariant();

    private static bool ContrasenaValida(string contrasena) =>
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

public sealed record ResultadoRegistroCliente(bool Registrado, string? Error, bool ConflictoExistente)
{
    public static ResultadoRegistroCliente Exitoso() => new(true, null, false);
    public static ResultadoRegistroCliente Invalido(string error) => new(false, error, false);
    public static ResultadoRegistroCliente Conflicto() => new(false, null, true);
}