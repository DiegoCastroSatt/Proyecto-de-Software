namespace Optica.Api.Modules.Clientes.DTOs;

public class ActualizarClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}