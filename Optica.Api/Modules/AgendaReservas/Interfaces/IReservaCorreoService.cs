using Optica.Api.Modules.AgendaReservas.DTOs;

namespace Optica.Api.Modules.AgendaReservas.Interfaces;

public interface IReservaCorreoService
{
    Task<bool> EnviarReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken);

    Task<bool> EnviarRecordatorio(ReservaCorreoDto reserva, CancellationToken cancellationToken);
}
