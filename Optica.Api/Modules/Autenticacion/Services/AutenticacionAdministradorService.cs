using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class AutenticacionAdministradorService(
    IAutenticacionAdministradorRepository administradorRepository,
    IPasswordHasher<Administrador> passwordHasher,
    EstadoRecuperacionAdministrador estadoRecuperacion,
    IEmailService emailService,
    TimeProvider proveedorTiempo,
    ILogger<AutenticacionAdministradorService> logger) : IAutenticacionAdministradorService
{
    private const string NombreCentro = "Centro Óptico San Francisco";

    public async Task<LoginAdministradorResponse?> IniciarSesionAsync(
        LoginAdministradorRequest request,
        CancellationToken cancellationToken)
    {
        var nombreUsuario = request.NombreUsuario.Trim();
        var administrador = await administradorRepository.ObtenerPorNombreUsuarioAsync(
            nombreUsuario,
            cancellationToken);

        if (administrador is null || administrador.Estado != "Activo")
        {
            return null;
        }

        if (EsHashIdentity(administrador.Contrasena))
        {
            var resultado = passwordHasher.VerifyHashedPassword(
                administrador,
                administrador.Contrasena,
                request.Contrasena);

            if (resultado == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {
                administrador.Contrasena = passwordHasher.HashPassword(administrador, request.Contrasena);
                await administradorRepository.ActualizarAsync(administrador, cancellationToken);
            }
        }
        else
        {
            var contrasenaAlmacenada = Encoding.UTF8.GetBytes(administrador.Contrasena);
            var contrasenaIngresada = Encoding.UTF8.GetBytes(request.Contrasena);

            if (!CryptographicOperations.FixedTimeEquals(contrasenaAlmacenada, contrasenaIngresada))
            {
                return null;
            }

            administrador.Contrasena = passwordHasher.HashPassword(administrador, request.Contrasena);
            await administradorRepository.ActualizarAsync(administrador, cancellationToken);
        }

        return new LoginAdministradorResponse(administrador.IdAdministrador, administrador.Nombre);
    }

    public async Task<bool> SolicitarRecuperacionAsync(
        string nombreUsuario,
        string? ipCliente,
        CancellationToken cancellationToken)
    {
        var nombre = nombreUsuario.Trim();
        var ahora = proveedorTiempo.GetUtcNow();
        if (!estadoRecuperacion.IntentarIniciarSolicitud(nombre, ipCliente, ahora))
            return false;

        var administrador = await administradorRepository.ObtenerPorNombreUsuarioAsync(nombre, cancellationToken);
        if (administrador is null || administrador.Estado != "Activo")
            return true;

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
        var asunto = $"Código para recuperar tu contraseña - {NombreCentro}";
        var codigoHtml = HtmlEncoder.Default.Encode(codigo);
        var cuerpoHtml = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Recuperación de contraseña</h1>
            <p>Usa este código para cambiar la contraseña de tu cuenta de administrador:</p>
            <p style="font-size:30px;font-weight:bold;letter-spacing:8px;color:#176b6c">{codigoHtml}</p>
            <p>El código vence en 5 minutos y solo puede utilizarse una vez.</p>
            <p>Si no solicitaste este cambio, puedes ignorar este correo.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;

        var resultado = await emailService.EnviarConResultadoAsync(
            administrador.Correo, asunto, cuerpoHtml, cancellationToken, importante: true);
        if (!resultado.Exitoso)
        {
            logger.LogWarning("No se pudo enviar el código de recuperación a la cuenta administrativa {AdministradorId}.", administrador.IdAdministrador);
            return true;
        }

        estadoRecuperacion.GuardarCodigo(
            nombre, administrador.IdAdministrador, estadoRecuperacion.CrearHashCodigo(codigo), ahora);
        return true;
    }

    public async Task<bool> ConfirmarRecuperacionAsync(
        string nombreUsuario,
        string codigo,
        string nuevaContrasena,
        string confirmarContrasena,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(nuevaContrasena, confirmarContrasena, StringComparison.Ordinal))
            throw new ArgumentException("La nueva contraseña y su confirmación no coinciden.");

        if (nuevaContrasena.Length is < 12 or > 255)
            throw new ArgumentException("La contraseña debe tener entre 12 y 255 caracteres.");

        var estadoCodigo = estadoRecuperacion.ValidarCodigo(
            nombreUsuario, estadoRecuperacion.CrearHashCodigo(codigo), proveedorTiempo.GetUtcNow());
        if (!estadoCodigo.Valido)
            return false;

        try
        {
            var administrador = await administradorRepository.ObtenerPorIdAsync(
                estadoCodigo.IdAdministrador, cancellationToken);
            if (administrador is null || administrador.Estado != "Activo"
                || !string.Equals(EstadoRecuperacionAdministrador.NormalizarNombre(administrador.Nombre),
                    EstadoRecuperacionAdministrador.NormalizarNombre(nombreUsuario), StringComparison.Ordinal))
            {
                estadoRecuperacion.FinalizarCambio(nombreUsuario, exitoso: true);
                return false;
            }

            administrador.Contrasena = passwordHasher.HashPassword(administrador, nuevaContrasena);
            await administradorRepository.ActualizarAsync(administrador, cancellationToken);
            estadoRecuperacion.FinalizarCambio(nombreUsuario, exitoso: true);
            return true;
        }
        catch
        {
            estadoRecuperacion.FinalizarCambio(nombreUsuario, exitoso: false);
            throw;
        }
    }

    private static bool EsHashIdentity(string contrasena) =>
        contrasena.StartsWith("AQAAAA", StringComparison.Ordinal);
}
