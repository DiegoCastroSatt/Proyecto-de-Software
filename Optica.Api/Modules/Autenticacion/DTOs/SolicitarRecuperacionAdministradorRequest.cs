using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record SolicitarRecuperacionAdministradorRequest
{
    [Required, StringLength(60)]
    public required string NombreUsuario { get; init; }
}
