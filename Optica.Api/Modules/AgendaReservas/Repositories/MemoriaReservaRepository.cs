using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
public class MemoriaReservaRepository : IReservaRepository
{
    private readonly List<Reserva> _reservas = [];
    private int _siguienteId;

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null) =>
        Task.FromResult<IReadOnlyList<Horario>>([]);

    public Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReservaAgendaResponseDto>>([]);

    public Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado) =>
        Task.FromResult(false);

    public Task<Reserva> CrearReserva(CrearReservaDto dto, string tokenConfirmacionHash, string tokenCancelacionHash) =>
        throw new NotSupportedException();

    public Task<ReservaCorreoDto?> ObtenerDatosCorreoReserva(int id, CancellationToken cancellationToken) => Task.FromResult<ReservaCorreoDto?>(null);

    public Task<ResultadoAccionReserva> EjecutarAccionPorToken(string tokenHash, bool confirmar, DateTime ahoraUtc, CancellationToken cancellationToken) =>
        Task.FromResult(new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido });

    public Task<IReadOnlyList<ReservaCorreoDto>> ObtenerReservasParaRecordatorio(DateTimeOffset ahoraChile, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReservaCorreoDto>>([]);

    public Task<bool> MarcarRecordatorioEnviado(int id, DateTime enviadoUtc, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task DesmarcarRecordatorio(int id, DateTime enviadoUtc, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora)
    {
        bool existe = _reservas.Any(reserva =>
            reserva.Estado != "Cancelada");

        return Task.FromResult(existe);
    }

    public Task<Reserva> Crear(Reserva reserva)
    {
        reserva.Id = Interlocked.Increment(ref _siguienteId);
        _reservas.Add(reserva);
        return Task.FromResult(reserva);
    }

    public Task<Reserva?> ObtenerPorId(int id)
    {
        return Task.FromResult(_reservas.FirstOrDefault(reserva => reserva.Id == id));
    }

    public Task<bool> CancelarReserva(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reserva = _reservas.FirstOrDefault(item => item.Id == id);
        if (reserva is null)
        {
            return Task.FromResult(false);
        }

        if (reserva.Estado is "Cancelada" or "Realizada")
        {
            throw new InvalidOperationException("La reserva no se puede cancelar en su estado actual.");
        }

        reserva.Estado = "Cancelada";
        return Task.FromResult(true);
    }

    public Task<bool> ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken) =>
        throw new NotSupportedException("La reprogramación requiere el repositorio persistente de reservas.");
}
