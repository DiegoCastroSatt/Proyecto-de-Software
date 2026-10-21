using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Optica.Api.Modules.Notificaciones.Services;
using Optica.Api.Modules.Pedidos.Models;

namespace Optica.Api.Modules.Pedidos.Services;

public sealed class CorreoPedidoBackgroundService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<CorreoPedidoBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcesarPendientes(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("Falló la recuperación de correos de pedidos ({TipoError}).", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task ProcesarPendientes(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var pendientes = await db.CorreosPedido.Where(c => !c.Enviado && c.ProximoIntentoUtc <= ahora)
            .OrderBy(c => c.CreadoUtc).ThenBy(c => c.Id).Take(50).ToListAsync(cancellationToken);

        foreach (var correo in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ahoraReclamo = timeProvider.GetUtcNow().UtcDateTime;
            var reclamado = await db.CorreosPedido.Where(c => c.Id == correo.Id && !c.Enviado
                    && c.ProximoIntentoUtc <= ahoraReclamo
                    && (c.ReclamadoHastaUtc == null || c.ReclamadoHastaUtc < ahoraReclamo))
                .ExecuteUpdateAsync(update => update.SetProperty(c => c.ReclamadoHastaUtc, ahoraReclamo.AddMinutes(2)), cancellationToken);
            if (reclamado != 1) continue;
            ResultadoEnvioCorreo resultado;
            try
            {
                resultado = await email.EnviarConResultadoAsync(correo.Destinatario, correo.Asunto,
                    correo.CuerpoHtml, cancellationToken, importante: correo.Importante);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await RegistrarResultadoInterrumpido(db, correo, timeProvider.GetUtcNow().UtcDateTime);
                throw;
            }
            catch (Exception ex) { resultado = new ResultadoEnvioCorreo(false, $"{ex.GetType().Name}: {ex.Message}"); }

            var fecha = timeProvider.GetUtcNow().UtcDateTime;
            correo.Intentos++;
            correo.Enviado = resultado.Exitoso;
            correo.ProximoIntentoUtc = resultado.Exitoso
                ? correo.ProximoIntentoUtc
                : fecha.Add(PoliticaReintentosCorreo.ObtenerPausa(correo.Intentos));
            db.IntentosCorreoPedido.Add(new IntentoCorreoPedido
            {
                CorreoPedidoId = correo.Id,
                FechaUtc = fecha,
                Exitoso = resultado.Exitoso,
                Error = resultado.Error
            });
            await db.SaveChangesAsync(CancellationToken.None);
            await db.CorreosPedido.Where(c => c.Id == correo.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(c => c.ReclamadoHastaUtc, (DateTime?)null), CancellationToken.None);
        }
    }

    private static async Task RegistrarResultadoInterrumpido(OpticaDbContext db, CorreoPedidoPendiente correo, DateTime fecha)
    {
        correo.Intentos++;
        correo.ProximoIntentoUtc = fecha.Add(PoliticaReintentosCorreo.ObtenerPausa(correo.Intentos));
        db.IntentosCorreoPedido.Add(new IntentoCorreoPedido
        {
            CorreoPedidoId = correo.Id,
            FechaUtc = fecha,
            Exitoso = false,
            Error = "El envío se interrumpió; el resultado SMTP es indeterminado y se reintentará."
        });
        await db.SaveChangesAsync(CancellationToken.None);
        await db.CorreosPedido.Where(c => c.Id == correo.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(c => c.ReclamadoHastaUtc, (DateTime?)null), CancellationToken.None);
    }
}
