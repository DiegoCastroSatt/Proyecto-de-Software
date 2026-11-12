using Optica.Api.Modules.Encuestas.DTOs;

namespace Optica.Api.Modules.Encuestas.Interfaces;

public enum ResultadoEncuesta
{
    Disponible,
    Respondida,
    NoEncontrada,
    Vencida,
    YaRespondida
}

public interface IEncuestaService
{
    Task<(ResultadoEncuesta Resultado, EncuestaPublicaDto? Encuesta)> ObtenerPorToken(string token, CancellationToken cancellationToken);

    /// <summary>Guarda la respuesta una sola vez. Lanza <see cref="ArgumentException"/> si los datos no son válidos.</summary>
    Task<ResultadoEncuesta> Responder(string token, ResponderEncuestaDto dto, CancellationToken cancellationToken);

    Task<ResumenEncuestasDto> ObtenerResumen(DateTime desde, DateTime hasta, CancellationToken cancellationToken);

    Task<ComentariosEncuestasPaginadosDto> ObtenerComentarios(
        DateTime desde,
        DateTime hasta,
        int? calificacion,
        bool soloConComentario,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken);

    /// <summary>Oculta o vuelve a mostrar el comentario de una respuesta. Devuelve false si no existe.</summary>
    Task<bool> CambiarVisibilidadComentario(int idRespuesta, bool oculto, CancellationToken cancellationToken);
}
