namespace Optica.Api.Modules.Pedidos.Interfaces;

using Optica.Api.Modules.Pedidos.Models;

public interface IPedidoCorreoService
{
    Task<bool> EnviarPedidoListoAsync(string? destinatario, string nombreCliente, int idPedido, CancellationToken cancellationToken);

    CorreoPedidoPendiente PrepararPedidoListo(string? destinatario, string nombreCliente, int idPedido) =>
        throw new NotSupportedException("El servicio no prepara notificaciones persistentes.");
}
