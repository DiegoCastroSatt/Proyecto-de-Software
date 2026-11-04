public class ActualizarStockDto
{
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public required string MotivoCambio { get; set; }
    public required string TipoCambio { get; set; }
}