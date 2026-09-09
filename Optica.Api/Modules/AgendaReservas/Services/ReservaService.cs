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
        // 1. Validar fecha
        if (dto.Fecha.Date < DateTime.Today)
        {
            throw new Exception(
                "No se puede reservar una fecha pasada."
            );
        }

        // 2. Comprobar si la hora ya está ocupada
        bool existe = await _reservaRepository
            .ExisteReserva(dto.Fecha, dto.Hora);

        if (existe)
        {
            throw new Exception(
                "La hora seleccionada ya está reservada."
            );
        }

        // 3. Crear Model
        var reserva = new Reserva
        {
            ClienteId = 1, // temporal
            Fecha = dto.Fecha,
            Hora = dto.Hora,
            Estado = "Pendiente"
        };

        // 4. Guardar
        var reservaCreada =
            await _reservaRepository.Crear(reserva);

        // 5. Crear respuesta
        return new ReservaResponseDto
        {
            Id = reservaCreada.Id,
            Fecha = reservaCreada.Fecha,
            Hora = reservaCreada.Hora,
            Estado = reservaCreada.Estado
        };
    }
}