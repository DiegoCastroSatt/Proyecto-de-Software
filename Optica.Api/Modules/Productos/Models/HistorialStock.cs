namespace Optica.Api.Modules.Productos.Models;

public class HistorialStock
{
    public int IdCambio { get; set; }
    public int IdProducto { get; set; }
    public string MotivoMovimiento { get; set; } = string.Empty;
    public int IdResponsable { get; set; }
    public string TipoStock { get; set; } = string.Empty;
    public int ValorAnterior { get; set; }
    public int ValorActual { get; set; }
    public DateTime FechaCambio { get; set; }
}