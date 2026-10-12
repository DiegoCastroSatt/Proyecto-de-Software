using Optica.Api.Modules.AgendaReservas.Interfaces;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed class RecordatorioReservasService(
    IReservaRepository reservaRepository,
    IReservaCorreoService correoService,
    ColaCorreosReprogramacion correosReprogramacion,
    TimeProvider proveedorTiempo,
    ILogger<RecordatorioReservasService> logger) : IRecordatorioReservasService
{
    public async Task EnviarPendientes(CancellationToken cancellationToken)
    {
        await ReintentarCorreosReprogramacion(cancellationToken);
        var ahoraChile = HoraChile.ObtenerAhora(proveedorTiempo);
        await reservaRepository.MarcarReservasVencidasComoRealizadas(ahoraChile, cancellationToken);
        var pendientes = await reservaRepository.ObtenerReservasParaRecordatorio(ahoraChile, cancellationToken);
        foreach (var reserva in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enviadoUtc = proveedorTiempo.GetUtcNow().UtcDateTime;
            if (!await reservaRepository.MarcarRecordatorioEnviado(reserva.Id, reserva.HorarioId, enviadoUtc, cancellationToken))
            {
                continue;
            }

            var enviado = false;
            try
            {
                enviado = await correoService.EnviarRecordatorio(reserva, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await reservaRepository.DesmarcarRecordatorio(reserva.Id, reserva.HorarioId, enviadoUtc, CancellationToken.None);
                throw;
            }
            catch (Exception)
            {
                logger.LogError("Falló el envío del recordatorio de la reserva {ReservaId}.", reserva.Id);
            }
            if (!enviado)
            {
                await reservaRepository.DesmarcarRecordatorio(reserva.Id, reserva.HorarioId, enviadoUtc, CancellationToken.None);
                logger.LogWarning("El recordatorio de la reserva {ReservaId} no se pudo enviar; se reintentará en la próxima ejecución.", reserva.Id);
            }
        }
    }

    private async Task ReintentarCorreosReprogramacion(CancellationToken cancellationToken)
    {
        var lote = correosReprogramacion.TomarLote();
        for (var indice = 0; indice < lote.Count; indice++)
        {
            var correoPendiente = lote[indice];
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ahoraUtc = proveedorTiempo.GetUtcNow().UtcDateTime;
                if (!await reservaRepository.SonTokensReprogramacionVigentes(
                        correoPendiente.ReservaId,
                        correoPendiente.TokenConfirmacionHash,
                        correoPendiente.TokenCancelacionHash,
                        ahoraUtc,
                        cancellationToken))
                {
                    continue;
                }

                var reserva = await reservaRepository.ObtenerDatosCorreoReserva(correoPendiente.ReservaId, cancellationToken);
                var enviado = reserva is not null && await correoService.EnviarReservaReprogramada(
                    reserva,
                    correoPendiente.TokenConfirmacion,
                    correoPendiente.TokenCancelacion,
                    cancellationToken);
                if (!enviado)
                {
                    correosReprogramacion.Encolar(correoPendiente);
                    logger.LogWarning("Sigue pendiente el correo de reprogramación de la reserva {ReservaId}; se reintentará en el próximo ciclo.", correoPendiente.ReservaId);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                for (var pendiente = indice; pendiente < lote.Count; pendiente++)
                {
                    correosReprogramacion.Encolar(lote[pendiente]);
                }
                throw;
            }
            catch (Exception)
            {
                correosReprogramacion.Encolar(correoPendiente);
                logger.LogError("Falló el reintento del correo de reprogramación de la reserva {ReservaId}.", correoPendiente.ReservaId);
            }
        }
    }
}
