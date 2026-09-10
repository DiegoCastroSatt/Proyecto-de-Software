using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("disponibles")]
    public async Task<IActionResult> ObtenerDisponibles()
    {
        var horarios = await _reservaService.ObtenerHorariosDisponibles();
        return Ok(horarios.Select(h => new HorarioDisponibleResponseDto
        {
            IdHorario = h.Id,
            Fecha = h.Fecha,
            Hora = h.HoraInicio
        }));
    }

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
}