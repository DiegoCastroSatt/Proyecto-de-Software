namespace Optica.Api.Modules.Clientes.DTOs;

public class CrearClienteDto
{
    public string Rut { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
}