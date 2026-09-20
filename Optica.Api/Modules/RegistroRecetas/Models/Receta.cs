namespace Optica.Api.Modules.RegistroRecetas.Models;

public class Receta
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string? Observaciones { get; set; }
    public string? ImagenPath { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public List<Graduacion> Graduaciones { get; set; } = [];
}