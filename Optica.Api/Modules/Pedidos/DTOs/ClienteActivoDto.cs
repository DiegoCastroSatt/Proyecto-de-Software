using System.Text.Json.Serialization;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class ClienteActivoDto
{
    [JsonPropertyName("Rut")]
    public string Rut { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}
