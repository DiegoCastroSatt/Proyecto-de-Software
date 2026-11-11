namespace Optica.Api.Modules.Encuestas.DTOs;

/// <summary>
/// Respuesta anónima para el personal: solo muestra el mes, no la fecha exacta.
/// Si el comentario está oculto, su texto no se envía.
/// </summary>
public sealed record ComentarioEncuestaDto(
    int Id,
    string Mes,
    int CalificacionGeneral,
    int CalificacionTrato,
    int CalificacionPuntualidad,
    string? Comentario,
    bool ComentarioOculto);

public sealed class CambiarVisibilidadComentarioDto
{
    public bool Oculto { get; set; }
}

public sealed record ComentariosEncuestasPaginadosDto(
    IReadOnlyList<ComentarioEncuestaDto> Elementos,
    int Total,
    int Pagina,
    int TamanoPagina);
