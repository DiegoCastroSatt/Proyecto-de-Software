using Microsoft.AspNetCore.Http; //Linea para que C# pueda encontrar el IFormFile

public class CrearRecetaDto
{
    public string Rut { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? Observaciones { get; set; }
    public string? GraduacionesJson { get; set; }
    public IFormFile? Imagen { get; set; }
}