namespace Optica.Api.Modules.Ventas.Models;

public class Venta
{
    public int? ClienteId { get; set; }
    public Optica.Api.Modules.Clientes.Models.Cliente? Cliente { get; set; }
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = [];
}
