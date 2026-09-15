namespace Optica.Api.Modules.Ventas.Models;

public class Venta
{
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = [];
}
