using Optica.Api.Modules.AgendaReservas.Models;
public interface IReservaRepository
{
    Task<bool> ExisteReserva(
        DateTime fecha,
        TimeSpan hora
    );

    Task<Reserva> Crear(Reserva reserva);

    Task<Reserva?> ObtenerPorId(int id);
}