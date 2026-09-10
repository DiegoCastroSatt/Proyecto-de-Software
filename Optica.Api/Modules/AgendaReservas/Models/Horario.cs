namespace Optica.Api.Modules.AgendaReservas.Models;

public class Horario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}