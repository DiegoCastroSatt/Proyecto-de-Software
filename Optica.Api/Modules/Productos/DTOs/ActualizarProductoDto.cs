using System.ComponentModel.DataAnnotations;

public class ActualizarProductoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }

    [StringLength(255)]
    public string? MotivoCambio { get; set; }

    public string Estado { get; set; } = "Disponible";
    public IFormFile? Imagen { get; set; }
}