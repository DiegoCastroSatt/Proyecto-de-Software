using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Models;
using System.Text.Encodings.Web;

[ApiController]
[Route("api/[controller]")]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservasController(
        IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [AllowAnonymous]
    [HttpGet("disponibles")]
    public async Task<IActionResult> ObtenerDisponibles(
        [FromQuery] string? fecha,
        [FromQuery] int? excluirReservaId,
        CancellationToken cancellationToken)
    {
        DateTime? fechaFiltro = null;
        if (!string.IsNullOrWhiteSpace(fecha))
        {
            if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaParseada))
            {
                return BadRequest(new { mensaje = "La fecha no tiene un formato válido." });
            }
            fechaFiltro = fechaParseada.Date;
        }
        var horarios = await _reservaService.ObtenerHorariosDisponibles(fechaFiltro, excluirReservaId);
        return Ok(horarios.Select(h => new HorarioDisponibleResponseDto
        {
            IdHorario = h.Id,
            Fecha = h.Fecha,
            Hora = h.HoraInicio,
            HoraFin = h.HoraFin
        }));
    }

    [Authorize]
    [HttpGet("agenda")]
    public async Task<ActionResult<IReadOnlyList<Optica.Api.Modules.AgendaReservas.DTOs.ReservaAgendaResponseDto>>> ObtenerAgenda(
        [FromQuery] bool historialAtendidas = false,
        CancellationToken cancellationToken = default)
    {
        var reservas = await _reservaService.ObtenerAgenda(historialAtendidas, cancellationToken);
        return Ok(reservas);
    }

    [Authorize]
    [HttpPut("{id:int}/reprogramar")]
    public async Task<IActionResult> ReprogramarReserva(int id, ReprogramarReservaDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _reservaService.ReprogramarReserva(id, dto.IdHorario, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CrearReserva(
        CrearReservaDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var reserva =
                await _reservaService.CrearReserva(dto, cancellationToken);

            return Ok(reserva);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("acciones/confirmar/{token}")]
    public Task<IActionResult> ConfirmarDesdeCorreo(string token, CancellationToken cancellationToken) =>
        MostrarResultadoAccion(token, confirmar: true, cancellationToken);

    [AllowAnonymous]
    [HttpGet("acciones/cancelar/{token}")]
    public Task<IActionResult> CancelarDesdeCorreo(string token, CancellationToken cancellationToken) =>
        MostrarResultadoAccion(token, confirmar: false, cancellationToken);

    private async Task<IActionResult> MostrarResultadoAccion(string token, bool confirmar, CancellationToken cancellationToken)
    {
        var resultado = await _reservaService.EjecutarAccionPorToken(token, confirmar, cancellationToken);
        Response.Headers.CacheControl = "no-store, no-cache";
        Response.Headers.Pragma = "no-cache";
        var mensaje = resultado.Tipo switch
        {
            TipoResultadoAccionReserva.Confirmada => "¡Hora confirmada! Tu hora quedó confirmada correctamente.",
            TipoResultadoAccionReserva.YaConfirmada => "Esta hora ya se encuentra confirmada.",
            TipoResultadoAccionReserva.Cancelada => "Hora cancelada. La hora quedó liberada para otro cliente.",
            TipoResultadoAccionReserva.YaCancelada => "Esta hora ya se encuentra cancelada.",
            TipoResultadoAccionReserva.TokenExpirado => "El enlace ha expirado.",
            TipoResultadoAccionReserva.TokenInvalido => "El enlace no es válido.",
            _ => "Esta hora ya no puede modificarse desde este enlace."
        };
        var fecha = resultado.Fecha?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CL"));
        var hora = resultado.HoraInicio?.ToString(@"hh\:mm");
        var detalle = fecha is not null && hora is not null
            ? $"<p>Fecha: <strong>{HtmlEncoder.Default.Encode(fecha)}</strong><br>Hora: <strong>{HtmlEncoder.Default.Encode(hora)}</strong></p>"
            : string.Empty;
        var titulo = confirmar ? "Confirmación de hora" : "Cancelación de hora";
        var html = $"""
            <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{titulo}</title></head>
            <body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c"><main style="box-sizing:border-box;width:min(100% - 32px,560px);margin:10vh auto;padding:36px;background:#fff;border-radius:16px;box-shadow:0 12px 40px #18333c1a;text-align:center">
            <p style="color:#176b6c;font-weight:bold">Centro Óptico San Francisco</p><h1 style="font-size:25px">{HtmlEncoder.Default.Encode(titulo)}</h1>
            <p style="font-size:18px;line-height:1.5">{HtmlEncoder.Default.Encode(mensaje)}</p>{detalle}
            <p style="margin-top:28px;color:#63777c">Gracias por preferirnos.</p></main></body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [Authorize]
    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> CancelarReserva(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _reservaService.CancelarReserva(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }
}
