using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Interfaces;

public interface IPedidoRepository
{
    Task<IEnumerable<Pedido>> ObtenerPedidosAsync();
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync();
    Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync();
    Task<IEnumerable<Cliente>> BuscarClientesPorRutAsync(string rut)
        => throw new NotSupportedException("La búsqueda de clientes por RUT no está implementada en este repositorio.");
    Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut);
    Task<Cliente?> ObtenerClientePorRutAsync(string rut);
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetallePorRutAsync(string rut)
        => throw new NotSupportedException("La consulta de historial por RUT no está implementada en este repositorio.");
    Task<Pedido> CrearPedidoAsync(Pedido pedido);
    Task<Pedido?> ObtenerPedidoPorIdAsync(int id);
    Task<Pedido?> ObtenerPedidoPorIdParaActualizarAsync(int id) => ObtenerPedidoPorIdAsync(id);
    Task<PedidoResponseDto?> ObtenerPedidoDetalleAsync(int id);
    Task ActualizarPedidoAsync(Pedido pedido);
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetallePorRutAsync(string rut);
}
