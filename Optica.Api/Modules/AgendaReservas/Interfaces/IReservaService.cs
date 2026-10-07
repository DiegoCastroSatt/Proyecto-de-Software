public interface IReservaService
{
    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.Models.Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null);

    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.DTOs.ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken);

    Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto
    );

    Task CancelarReserva(int id, CancellationToken cancellationToken);

    Task ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken);
}
