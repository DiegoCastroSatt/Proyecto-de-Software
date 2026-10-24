using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record LoginClienteRequest
{
    [Required, StringLength(100)]
    public required string Correo { get; init; }

    [Required, StringLength(255)]
    public required string Contrasena { get; init; }
}
