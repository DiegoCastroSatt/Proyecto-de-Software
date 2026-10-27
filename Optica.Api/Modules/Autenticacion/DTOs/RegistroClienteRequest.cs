using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record RegistroClienteRequest
{
    [Required, StringLength(60)] public required string Nombre { get; init; }
    [Required, StringLength(60)] public required string Apellido { get; init; }
    [Required, StringLength(12)] public required string Rut { get; init; }
    [Required, EmailAddress, StringLength(100)] public required string Correo { get; init; }
    [Required, StringLength(20)] public required string Telefono { get; init; }
    [Required, StringLength(255)] public required string Contrasena { get; init; }
    [Required, StringLength(255)] public required string ConfirmarContrasena { get; init; }
}
