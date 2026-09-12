namespace Optica.Api.Modules.RegistroRecetas.Models;

public class Graduacion
{
    public int Id { get; set; }
    public int RecetaId { get; set; }
    public string Ojo { get; set; } = string.Empty; // Ojo derecho y ojo izquierdo
    public decimal? Esfera { get; set; }
    public decimal? Cilindro { get; set; }
    public int? Eje { get; set; }
    public decimal? Adicion { get; set; }
}