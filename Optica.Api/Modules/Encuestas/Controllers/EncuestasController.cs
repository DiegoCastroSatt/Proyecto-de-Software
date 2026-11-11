using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Encuestas.DTOs;
using Optica.Api.Modules.Encuestas.Interfaces;

namespace Optica.Api.Modules.Encuestas.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EncuestasController(IEncuestaService encuestas) : ControllerBase
{
    private const string MensajeNoEncontrada = "Este enlace no es válido o la encuesta ya fue respondida.";
    private const string MensajeVencida = "Este enlace venció.";
    private const int DiasMaximosRango = 366 * 3;

    /// <summary>Datos de la atención para mostrar el formulario. Público: el cliente responde sin iniciar sesión.</summary>
    [AllowAnonymous]
    [HttpGet("responder/{token}")]
    public async Task<IActionResult> Obtener(string token, CancellationToken cancellationToken)
    {
        NoGuardarEnCache();
        var (resultado, encuesta) = await encuestas.ObtenerPorToken(token, cancellationToken);
        return resultado switch
        {
            ResultadoEncuesta.Disponible => Ok(encuesta),
            ResultadoEncuesta.Vencida => StatusCode(StatusCodes.Status410Gone, new { mensaje = MensajeVencida }),
            _ => NotFound(new { mensaje = MensajeNoEncontrada })
        };
    }

    [AllowAnonymous]
    [HttpPost("responder/{token}")]
    public async Task<IActionResult> Responder(string token, [FromBody] ResponderEncuestaDto dto, CancellationToken cancellationToken)
    {
        NoGuardarEnCache();
        try
        {
            return await encuestas.Responder(token, dto, cancellationToken) switch
            {
                ResultadoEncuesta.Respondida => Ok(new { mensaje = "Gracias por tu opinión." }),
                ResultadoEncuesta.Vencida => StatusCode(StatusCodes.Status410Gone, new { mensaje = MensajeVencida }),
                ResultadoEncuesta.YaRespondida => Conflict(new { mensaje = MensajeNoEncontrada }),
                _ => NotFound(new { mensaje = MensajeNoEncontrada })
            };
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [Authorize(Policy = "agenda")]
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen([FromQuery] string? desde, [FromQuery] string? hasta, CancellationToken cancellationToken)
    {
        if (!TryLeerRango(desde, hasta, out var inicio, out var fin, out var error)) return BadRequest(new { mensaje = error });
        return Ok(await encuestas.ObtenerResumen(inicio, fin, cancellationToken));
    }

    [Authorize(Policy = "agenda")]
    [HttpGet("comentarios")]
    public async Task<IActionResult> Comentarios(
        [FromQuery] string? desde,
        [FromQuery] string? hasta,
        [FromQuery] int? calificacion,
        [FromQuery] bool soloConComentario = true,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryLeerRango(desde, hasta, out var inicio, out var fin, out var error)) return BadRequest(new { mensaje = error });
        try
        {
            return Ok(await encuestas.ObtenerComentarios(
                inicio, fin, calificacion, soloConComentario, pagina, tamanoPagina, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>Oculta o vuelve a mostrar un comentario inapropiado. Las calificaciones siguen contando.</summary>
    [Authorize(Policy = "agenda")]
    [HttpPut("respuestas/{idRespuesta:int}/comentario-oculto")]
    public async Task<IActionResult> CambiarVisibilidadComentario(
        int idRespuesta, [FromBody] CambiarVisibilidadComentarioDto dto, CancellationToken cancellationToken)
    {
        return await encuestas.CambiarVisibilidadComentario(idRespuesta, dto.Oculto, cancellationToken)
            ? NoContent()
            : NotFound(new { mensaje = "La respuesta no existe." });
    }

    private void NoGuardarEnCache() => Response.Headers.CacheControl = "no-store";

    private static bool TryLeerRango(string? desde, string? hasta, out DateTime inicio, out DateTime fin, out string error)
    {
        fin = default;
        error = string.Empty;
        if (!TryLeerFecha(desde, out inicio) || !TryLeerFecha(hasta, out fin))
        {
            error = "Las fechas deben tener el formato yyyy-MM-dd.";
            return false;
        }
        if (inicio > fin)
        {
            error = "La fecha de inicio no puede ser posterior a la fecha de término.";
            return false;
        }
        if ((fin - inicio).TotalDays > DiasMaximosRango)
        {
            error = "El periodo consultado no puede superar tres años.";
            return false;
        }
        return true;
    }

    private static bool TryLeerFecha(string? valor, out DateTime fecha) =>
        DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
}
