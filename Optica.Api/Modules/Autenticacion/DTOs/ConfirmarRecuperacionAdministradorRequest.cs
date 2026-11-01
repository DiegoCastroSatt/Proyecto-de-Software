using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record ConfirmarRecuperacionAdministradorRequest
{
    [Required, StringLength(60)]
    public required string NombreUsuario { get; init; }

    [Required, RegularExpression("^[0-9]{6}$")]
    public required string Codigo { get; init; }

    [Required, StringLength(255, MinimumLength = 8)]
    public required string NuevaContrasena { get; init; }

    [Required, StringLength(255, MinimumLength = 8)]
    public required string ConfirmarContrasena { get; init; }
}
