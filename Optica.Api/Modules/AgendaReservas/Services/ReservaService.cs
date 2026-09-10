using Optica.Api.Modules.AgendaReservas.Models;
public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservaRepository;

    public ReservaService(
        IReservaRepository reservaRepository)
    {
        _reservaRepository = reservaRepository;
    }

    public async Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto) || string.IsNullOrWhiteSpace(dto.Rut) || dto.IdHorario <= 0)
        {
            throw new ArgumentException("Los datos del cliente y el horario son obligatorios.");
        }

        var reservaCreada = await _reservaRepository.CrearReserva(dto);
        return new ReservaResponseDto
        {
            Id = reservaCreada.Id,
            IdHorario = reservaCreada.HorarioId,
            Fecha = reservaCreada.Fecha,
            Hora = reservaCreada.Hora,
            Estado = reservaCreada.Estado
        };
    }

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() =>
        _reservaRepository.ObtenerHorariosDisponibles();
}