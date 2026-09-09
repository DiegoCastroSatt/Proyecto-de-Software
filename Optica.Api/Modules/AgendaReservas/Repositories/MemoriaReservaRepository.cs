using Optica.Api.Modules.AgendaReservas.Models;
public class MemoriaReservaRepository : IReservaRepository
{
    private readonly List<Reserva> _reservas = [];
    private int _siguienteId;

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora)
    {
        bool existe = _reservas.Any(reserva =>
            reserva.Fecha.Date == fecha.Date &&
            reserva.Hora == hora &&
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