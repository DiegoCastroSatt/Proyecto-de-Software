namespace Optica.Api.Modules.Encuestas.Models;

/// <summary>Respuesta anónima: no tiene referencia a la reserva, al cliente ni a la encuesta.</summary>
public sealed class RespuestaEncuestaAtencion
{
    public int Id { get; set; }
    public int CalificacionGeneral { get; set; }
    public int CalificacionTrato { get; set; }
    public int CalificacionPuntualidad { get; set; }
    public string? Comentario { get; set; }
    /// <summary>El personal puede ocultar comentarios inapropiados; las calificaciones siguen contando.</summary>
    public bool ComentarioOculto { get; set; }
    /// <summary>Día de la respuesta en hora de Chile, sin hora.</summary>
    public DateTime FechaRespuesta { get; set; }
}
