public interface IReservaService
{
    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.Models.Horario>> ObtenerHorariosDisponibles();

    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.DTOs.ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken);

    Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto
    );

    Task CancelarReserva(int id, CancellationToken cancellationToken);
}
