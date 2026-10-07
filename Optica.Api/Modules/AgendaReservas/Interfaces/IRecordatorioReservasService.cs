namespace Optica.Api.Modules.AgendaReservas.Interfaces;

public interface IRecordatorioReservasService
{
    Task EnviarPendientes(CancellationToken cancellationToken);
}
