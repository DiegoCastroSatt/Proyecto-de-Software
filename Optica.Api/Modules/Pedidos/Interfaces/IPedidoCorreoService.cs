namespace Optica.Api.Modules.Pedidos.Interfaces;

public interface IPedidoCorreoService
{
    Task<bool> EnviarPedidoListoAsync(string? destinatario, string nombreCliente, int idPedido, CancellationToken cancellationToken);
}
