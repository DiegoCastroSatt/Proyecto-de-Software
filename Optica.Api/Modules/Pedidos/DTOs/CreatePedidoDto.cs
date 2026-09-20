using System;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class CreatePedidoDto
{
    public string NombreCliente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Anotaciones { get; set; }
}
