public interface IReservaService
{
    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.Models.Horario>> ObtenerHorariosDisponibles();

    Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto
    );
}