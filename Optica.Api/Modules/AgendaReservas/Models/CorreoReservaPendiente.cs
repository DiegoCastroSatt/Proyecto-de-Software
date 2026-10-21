namespace Optica.Api.Modules.AgendaReservas.Models;

public sealed class CorreoReservaPendiente
{
    public long Id { get; set; }
    public int ReservaId { get; set; }
    public int HorarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string CuerpoHtmlProtegido { get; set; } = string.Empty;
    public byte[]? Adjunto { get; set; }
    public string? NombreAdjunto { get; set; }
    public string? TipoAdjunto { get; set; }
    public bool Enviado { get; set; }
    public int Intentos { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime ProximoIntentoUtc { get; set; }
    public DateTime? ReclamadoHastaUtc { get; set; }
    public ICollection<IntentoCorreoReserva> Historial { get; set; } = new List<IntentoCorreoReserva>();
}

public sealed class IntentoCorreoReserva
{
    public long Id { get; set; }
    public long CorreoReservaId { get; set; }
    public DateTime FechaUtc { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
