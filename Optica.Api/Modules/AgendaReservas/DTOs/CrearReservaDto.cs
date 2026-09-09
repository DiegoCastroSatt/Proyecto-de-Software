public class CrearReservaDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rut { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public TimeSpan Hora { get; set; }
}