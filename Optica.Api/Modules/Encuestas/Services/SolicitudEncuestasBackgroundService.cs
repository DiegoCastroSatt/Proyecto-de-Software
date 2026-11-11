using Optica.Api.Modules.Encuestas.Interfaces;

namespace Optica.Api.Modules.Encuestas.Services;

/// <summary>Revisa periódicamente las atenciones realizadas y programa sus encuestas.</summary>
public sealed class SolicitudEncuestasBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SolicitudEncuestasBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var servicio = scope.ServiceProvider.GetRequiredService<ISolicitudEncuestasService>();
                await servicio.ProgramarPendientes(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Falló la programación de encuestas de atención ({TipoError}).", ex.GetType().Name);
            }
        }
        while (await temporizador.WaitForNextTickAsync(stoppingToken));
    }
}
