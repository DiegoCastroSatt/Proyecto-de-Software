using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
public class MemoriaReservaRepository : IReservaRepository
{
    private readonly List<Reserva> _reservas = [];
    private int _siguienteId;

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() =>
        Task.FromResult<IReadOnlyList<Horario>>([]);

    public Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReservaAgendaResponseDto>>([]);

    public Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado) =>
        Task.FromResult(false);

    public Task<Reserva> CrearReserva(CrearReservaDto dto) =>
        throw new NotSupportedException();

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
}
