using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed class RecordatorioReservasBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RecordatorioReservasBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var servicio = scope.ServiceProvider.GetRequiredService<Optica.Api.Modules.AgendaReservas.Interfaces.IRecordatorioReservasService>();
                await servicio.EnviarPendientes(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                logger.LogError("Falló la revisión periódica de recordatorios de reservas.");
            }
        }
        while (await temporizador.WaitForNextTickAsync(stoppingToken));
    }
}
