using System.Collections.Concurrent;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed record CorreoReprogramacionPendiente(
    int ReservaId,
    string TokenConfirmacion,
    string TokenCancelacion,
    string TokenConfirmacionHash,
    string TokenCancelacionHash);

/// <summary>
/// Conserva en memoria las notificaciones que requieren reintento durante la vida del proceso.
/// Los tokens no se registran y solo se usan para volver a enviar el correo de la versión vigente.
/// </summary>
public sealed class ColaCorreosReprogramacion
{
    private readonly ConcurrentQueue<CorreoReprogramacionPendiente> pendientes = new();

    public void Encolar(CorreoReprogramacionPendiente correo) => pendientes.Enqueue(correo);

    public IReadOnlyList<CorreoReprogramacionPendiente> TomarLote()
    {
        var cantidad = pendientes.Count;
        var lote = new List<CorreoReprogramacionPendiente>(cantidad);
        for (var indice = 0; indice < cantidad && pendientes.TryDequeue(out var correo); indice++)
        {
            lote.Add(correo);
        }
        return lote;
    }

    public int Cantidad => pendientes.Count;
}
