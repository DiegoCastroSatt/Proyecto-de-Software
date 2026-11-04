public class CrearProductoDto
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
    public required string MotivoCambio { get; set; } = "Registro de producto nuevo";
    public required string TipoCambio { get; set; }
    public string Estado { get; set; } = "Disponible";
    public IFormFile? Imagen { get; set; }
}