namespace Optica.Api.Modules.Encuestas.DTOs;

public sealed class ResponderEncuestaDto
{
    public int CalificacionGeneral { get; set; }
    public int CalificacionTrato { get; set; }
    public int CalificacionPuntualidad { get; set; }
    public string? Comentario { get; set; }
}
