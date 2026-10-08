using Optica.Api.Modules.AgendaReservas.Models;

public interface IReservaService
{
    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.Models.Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null);

    Task<IReadOnlyList<Optica.Api.Modules.AgendaReservas.DTOs.ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken);

    Task<ReservaResponseDto> CrearReserva(
        CrearReservaDto dto,
        CancellationToken cancellationToken = default
    );

    Task<ResultadoAccionReserva> EjecutarAccionPorToken(string token, bool confirmar, CancellationToken cancellationToken);

    Task CancelarReserva(int id, CancellationToken cancellationToken);

    Task ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken);
}
