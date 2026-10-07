namespace Optica.Api.Modules.AgendaReservas.DTOs;

public class ReservaCorreoDto
{
    public int Id { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string? CorreoCliente { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}
