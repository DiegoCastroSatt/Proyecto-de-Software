using System.ComponentModel.DataAnnotations.Schema;

namespace Optica.Api.Modules.Productos.Models;

public class HistorialStock
{
    public int IdCambio { get; set; }
    public int IdProducto { get; set; }
    public int IdTrabajador { get; set; }
    public int StockAnterior { get; set; }
    public int StockActual { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.Now;
    public string? MotivoCambio { get; set; }

    [ForeignKey("IdProducto")]
    public Producto Producto { get; set; } = null!;
}