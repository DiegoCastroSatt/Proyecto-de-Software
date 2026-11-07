using System.ComponentModel.DataAnnotations;

public class ActualizarStockDto
{
    public int Stock { get; set; }
    public int StockMinimo { get; set; }

    [Required, StringLength(255)]
    public string MotivoCambio { get; set; } = string.Empty;
}