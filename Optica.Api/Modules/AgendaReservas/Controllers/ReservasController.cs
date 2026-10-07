using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Optica.Api.Modules.AgendaReservas.DTOs;

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
        CrearReservaDto dto)
    {
        try
        {
            var reserva =
                await _reservaService.CrearReserva(dto);

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
