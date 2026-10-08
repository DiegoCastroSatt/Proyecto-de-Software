using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
public interface IReservaRepository
{
    Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null);

    Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken);

    Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado);

    Task<Reserva> CrearReserva(CrearReservaDto dto, string tokenConfirmacionHash, string tokenCancelacionHash);

    Task<ReservaCorreoDto?> ObtenerDatosCorreoReserva(int id, CancellationToken cancellationToken);

    Task<ResultadoAccionReserva> EjecutarAccionPorToken(string tokenHash, bool confirmar, DateTime ahoraUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservaCorreoDto>> ObtenerReservasParaRecordatorio(DateTimeOffset ahoraChile, CancellationToken cancellationToken);

    Task<bool> MarcarRecordatorioEnviado(int id, DateTime enviadoUtc, CancellationToken cancellationToken);

    Task DesmarcarRecordatorio(int id, DateTime enviadoUtc, CancellationToken cancellationToken);


    Task<bool> ExisteReserva(
        DateTime fecha,
        TimeSpan hora
    );

    Task<Reserva> Crear(Reserva reserva);

    Task<Reserva?> ObtenerPorId(int id);

    Task<bool> CancelarReserva(int id, CancellationToken cancellationToken);

    Task<bool> ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken);
}
