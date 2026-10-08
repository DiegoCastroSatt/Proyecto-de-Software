public class RecetaResponseDto
{
    public string Rut { get; set; } = string.Empty;
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public string? ImagenUrl { get; set; }
    public List<GraduacionDto> Graduaciones { get; set; } = [];
}