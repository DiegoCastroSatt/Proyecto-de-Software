namespace Optica.Api.Modules.Clientes.Models;

public class Cliente
{
    public int IdCliente { get; set; }

    public string Rut { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Apellido { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string Estado { get; set; } = "Activo";

    public DateTime FechaRegistro { get; set; }
}