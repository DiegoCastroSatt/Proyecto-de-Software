namespace Optica.Api.Modules.Encuestas.Models;

/// <summary>
/// Encuesta enviada por una atención. Controla el enlace de un solo uso y si ya se respondió,
/// pero no guarda el contenido de la respuesta para mantenerla anónima.
/// </summary>
public sealed class EncuestaAtencion
{
    public int Id { get; set; }
    public int ReservaId { get; set; }
    public string? TokenHash { get; set; }
    public DateTime? TokenExpiraUtc { get; set; }
    /// <summary>Momento programado para enviar el correo de la encuesta.</summary>
    public DateTime FechaEnvioUtc { get; set; }
    public bool Respondida { get; set; }
}
