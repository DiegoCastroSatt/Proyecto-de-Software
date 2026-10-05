namespace Optica.Api.Modules.AgendaReservas.DTOs;

public class ReservaAgendaResponseDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string RutCliente { get; set; } = string.Empty;
    public string? TelefonoCliente { get; set; }
    public string? CorreoCliente { get; set; }
}
