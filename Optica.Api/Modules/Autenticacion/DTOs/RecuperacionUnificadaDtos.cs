using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record SolicitarRecuperacionRequest
{
    [Required, StringLength(100)] public required string Identificador { get; init; }
}

public sealed record ConfirmarRecuperacionRequest
{
    [Required, StringLength(100)] public required string Identificador { get; init; }
    [Required, RegularExpression("^[0-9]{6}$")] public required string Codigo { get; init; }
    [Required, StringLength(255, MinimumLength = 8)] public required string NuevaContrasena { get; init; }
    [Required, StringLength(255, MinimumLength = 8)] public required string ConfirmarContrasena { get; init; }
}
