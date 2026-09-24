using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Interfaces;

public interface IPedidoService
{
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosAsync();
    Task<IEnumerable<ClienteActivoDto>> ObtenerClientesActivosAsync();
    Task<PedidoResponseDto> CrearPedidoAsync(CreatePedidoDto dto);
    Task<PedidoResponseDto> ActualizarEstadoAsync(int id, UpdateEstadoPedidoDto dto);
}
