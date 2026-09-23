using System;

namespace Optica.Api.Modules.Pedidos.Models;

public class Pedido
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public int? IdReceta { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public decimal Total { get; set; }
    public string? Anotaciones { get; set; }
}
