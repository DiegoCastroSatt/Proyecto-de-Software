using System;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class PedidoResponseDto
{
    public int IdPedido { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? Anotaciones { get; set; }
}
