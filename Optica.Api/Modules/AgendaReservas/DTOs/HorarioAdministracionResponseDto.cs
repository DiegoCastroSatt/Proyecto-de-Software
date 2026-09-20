namespace Optica.Api.Modules.AgendaReservas.DTOs;

public class HorarioAdministracionResponseDto
{
    public int IdHorario { get; set; }
    public DateTime Fecha { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
