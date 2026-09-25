using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class AutenticacionAdministradorService(
    IAutenticacionAdministradorRepository administradorRepository,
    IPasswordHasher<Administrador> passwordHasher) : IAutenticacionAdministradorService
{
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

    private static bool EsHashIdentity(string contrasena) =>
        contrasena.StartsWith("AQAAAA", StringComparison.Ordinal);
}