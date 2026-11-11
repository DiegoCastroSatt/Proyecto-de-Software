using Optica.Api.Modules.AvisosReposicion.Interfaces;

namespace Optica.Api.Modules.AvisosReposicion.Services;

public sealed class AvisoReposicionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AvisoReposicionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var servicio = scope.ServiceProvider.GetRequiredService<IAvisoReposicionService>();
                await servicio.ProcesarReposicionesDisponibles(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Falló la recuperación de avisos de reposición ({TipoError}).",
                    ex.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
