namespace Optica.Api.Modules.AgendaReservas.DTOs;

public class CrearHorariosDto
{
    public string Fecha { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public int DuracionMinutos { get; set; } = 20;
    public int? IdAdministrador { get; set; }
}
