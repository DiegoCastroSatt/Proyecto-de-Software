namespace Optica.Api.Modules.Ventas.Models;

public class DetalleVenta
{
    public int IdDetalle { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
