using Optica.Api.Modules.AgendaReservas.Models;
public class MemoriaReservaRepository : IReservaRepository
{
    private readonly List<Reserva> _reservas = [];
    private int _siguienteId;

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() =>
        Task.FromResult<IReadOnlyList<Horario>>([]);

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
}