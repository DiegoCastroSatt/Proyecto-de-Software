public class GraduacionHistorialDto
{
    public string Ojo { get; set; } = string.Empty;
    public decimal? Esfera { get; set; }
    public decimal? Cilindro { get; set; }
    public int? Eje { get; set; }
    public decimal? Adicion { get; set; }
}

public class RecetaHistorialDto
{
    public string? ImagenUrl { get; set; }
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public List<GraduacionHistorialDto> Graduaciones { get; set; } = [];
}