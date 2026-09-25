using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Interfaces;

public interface IPedidoRepository
{
    Task<IEnumerable<Pedido>> ObtenerPedidosAsync();
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync();
    Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync();
    Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut);
    Task<Pedido> CrearPedidoAsync(Pedido pedido);
    Task<Pedido?> ObtenerPedidoPorIdAsync(int id);
    Task ActualizarPedidoAsync(Pedido pedido);
}
