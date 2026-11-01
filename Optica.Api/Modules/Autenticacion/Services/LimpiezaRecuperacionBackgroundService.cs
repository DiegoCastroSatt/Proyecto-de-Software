using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed class LimpiezaRecuperacionBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<LimpiezaRecuperacionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
                var ahora = timeProvider.GetUtcNow().UtcDateTime;
                await db.DesafiosRecuperacion
                    .Where(d => d.Estado == "Pendiente" && d.ExpiraUtc <= ahora)
                    .ExecuteUpdateAsync(u => u.SetProperty(d => d.Estado, "Expirado"), stoppingToken);
                await db.LimitesRecuperacion
                    .Where(l => l.InicioVentanaUtc <= ahora.AddDays(-1))
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError("Falló la limpieza de desafíos de recuperación ({TipoError}).", ex.GetType().Name);
            }

            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
