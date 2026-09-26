using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

/// <summary>Credenciales para iniciar sesión como administrador.</summary>
public sealed record LoginAdministradorRequest
{
    /// <summary>Nombre de usuario único del administrador.</summary>
    [Required, StringLength(60)]
    public required string NombreUsuario { get; init; }

    /// <summary>Contraseña ingresada por el administrador.</summary>
    [Required, StringLength(255)]
    public required string Contrasena { get; init; }
}