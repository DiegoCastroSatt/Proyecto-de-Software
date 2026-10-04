using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
public interface IReservaRepository
{
    Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles();

    Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken);

    Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado);

    Task<Reserva> CrearReserva(CrearReservaDto dto);

    Task<bool> ExisteReserva(
        DateTime fecha,
        TimeSpan hora
    );

    Task<Reserva> Crear(Reserva reserva);

    Task<Reserva?> ObtenerPorId(int id);

    Task<bool> CancelarReserva(int id, CancellationToken cancellationToken);
}
