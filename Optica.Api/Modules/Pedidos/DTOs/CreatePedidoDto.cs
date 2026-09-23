using System;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class CreatePedidoDto
{
    public string Rut { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public decimal Total { get; set; }
    public string? Anotaciones { get; set; }
}
