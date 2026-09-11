namespace Optica.Api.Modules.AgendaReservas.Models;
public class Reserva
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int HorarioId { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan Hora { get; set; }
    public string Estado { get; set; } = "Pendiente";
}