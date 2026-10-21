using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Models;

namespace Optica.Api.Modules.AgendaReservas.Interfaces;

public interface IReservaCorreoService
{
    Task<bool> EnviarReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken);

    Task<bool> EnviarReservaReprogramada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken);

    Task<bool> EnviarRecordatorio(ReservaCorreoDto reserva, CancellationToken cancellationToken);

    CorreoReservaPendiente PrepararReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion) =>
        throw new NotSupportedException("El servicio no prepara notificaciones persistentes.");

    CorreoReservaPendiente PrepararReservaReprogramada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion) =>
        throw new NotSupportedException("El servicio no prepara notificaciones persistentes.");

    CorreoReservaPendiente PrepararRecordatorio(ReservaCorreoDto reserva) =>
        throw new NotSupportedException("El servicio no prepara notificaciones persistentes.");
}
