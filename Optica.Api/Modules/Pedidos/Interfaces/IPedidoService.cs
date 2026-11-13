using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Interfaces;

public interface IPedidoService
{
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosAsync();
    Task<PedidosPaginaResponseDto> ObtenerPaginaAsync(int pagina, int tamano, string termino)
        => throw new NotSupportedException("El listado paginado de pedidos no está implementado.");
    Task<IEnumerable<ClienteActivoDto>> ObtenerClientesActivosAsync();
    Task<IEnumerable<ClienteActivoDto>> BuscarClientesPorRutAsync(string rut)
        => throw new NotSupportedException("La búsqueda de clientes por RUT no está implementada en este servicio.");
    Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosPorRutAsync(string rut)
        => throw new NotSupportedException("La consulta del historial por RUT no está implementada en este servicio.");
    Task<PedidoResponseDto> CrearPedidoAsync(CreatePedidoDto dto);
    Task<PedidoResponseDto?> ObtenerPedidoPorIdAsync(int id);
    Task<PedidoResponseDto> ActualizarEstadoAsync(int id, UpdateEstadoPedidoDto dto);
}
