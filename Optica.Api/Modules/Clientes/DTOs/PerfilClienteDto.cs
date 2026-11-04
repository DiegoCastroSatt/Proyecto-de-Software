using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Clientes.DTOs;

public sealed class PerfilClienteDto
{
    public string Rut { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public string? Telefono { get; init; }
    public string? Correo { get; init; }
    public string Estado { get; init; } = string.Empty;
}

public sealed class ActualizarPerfilClienteDto
{
    [Required, StringLength(60, MinimumLength = 1)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(60, MinimumLength = 1)]
    public string Apellido { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }
}
