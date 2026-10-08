using Optica.Api.Modules.AgendaReservas.Interfaces;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed class RecordatorioReservasService(
    IReservaRepository reservaRepository,
    IReservaCorreoService correoService,
    TimeProvider proveedorTiempo,
    ILogger<RecordatorioReservasService> logger) : IRecordatorioReservasService
{
    public async Task EnviarPendientes(CancellationToken cancellationToken)
    {
        var ahoraChile = HoraChile.ObtenerAhora(proveedorTiempo);
        var pendientes = await reservaRepository.ObtenerReservasParaRecordatorio(ahoraChile, cancellationToken);
        foreach (var reserva in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var enviadoUtc = proveedorTiempo.GetUtcNow().UtcDateTime;
            if (!await reservaRepository.MarcarRecordatorioEnviado(reserva.Id, enviadoUtc, cancellationToken))
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
                await reservaRepository.DesmarcarRecordatorio(reserva.Id, enviadoUtc, CancellationToken.None);
                throw;
            }
            catch (Exception)
            {
                logger.LogError("Falló el envío del recordatorio de la reserva {ReservaId}.", reserva.Id);
            }
            if (!enviado)
            {
                await reservaRepository.DesmarcarRecordatorio(reserva.Id, enviadoUtc, CancellationToken.None);
                logger.LogWarning("El recordatorio de la reserva {ReservaId} no se pudo enviar; se reintentará en la próxima ejecución.", reserva.Id);
            }
        }
    }
}
