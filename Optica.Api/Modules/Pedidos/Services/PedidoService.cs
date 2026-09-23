using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Models;

namespace Optica.Api.Modules.Pedidos.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidoRepository;

    public PedidoService(IPedidoRepository pedidoRepository)
    {
        _pedidoRepository = pedidoRepository;
    }

    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosAsync()
    {
        return await _pedidoRepository.ObtenerPedidosDetalleAsync();
    }

    public async Task<IEnumerable<ClienteActivoDto>> ObtenerClientesActivosAsync()
    {
        var clientes = await _pedidoRepository.ObtenerClientesActivosAsync();
        return clientes.Select(c => new ClienteActivoDto
        {
            Rut = c.Rut,
            NombreCompleto = c.Nombre + " " + c.Apellido
        });
    }

    public async Task<PedidoResponseDto> CrearPedidoAsync(CreatePedidoDto dto)
    {
        var cliente = await _pedidoRepository.ObtenerClienteActivoPorRutAsync(dto.Rut);
        if (cliente == null)
        {
            throw new ArgumentException("No se encontró un cliente activo con ese RUT.");
        }

        var pedido = new Pedido
        {
            Rut = dto.Rut,
            Fecha = dto.Fecha,
            Anotaciones = dto.Anotaciones,
            Estado = dto.Estado,
            Total = dto.Total
        };

        var nuevoPedido = await _pedidoRepository.CrearPedidoAsync(pedido);

        return new PedidoResponseDto
        {
            IdPedido = nuevoPedido.IdPedido,
            NombreCliente = cliente.Nombre + " " + cliente.Apellido,
            Fecha = nuevoPedido.Fecha,
            Estado = nuevoPedido.Estado,
            Total = nuevoPedido.Total,
            Anotaciones = nuevoPedido.Anotaciones
        };
    }

    public async Task<PedidoResponseDto> ActualizarEstadoAsync(int id, UpdateEstadoPedidoDto dto)
    {
        var pedido = await _pedidoRepository.ObtenerPedidoPorIdAsync(id);
        if (pedido == null)
        {
            throw new KeyNotFoundException("Pedido no encontrado.");
        }

        var estadosValidos = new[] { "Pendiente", "En proceso", "Listo", "Entregado", "Cancelado" };
        if (!estadosValidos.Contains(dto.Estado))
        {
            throw new ArgumentException("Estado no válido.");
        }

        pedido.Estado = dto.Estado;
        await _pedidoRepository.ActualizarPedidoAsync(pedido);

        // Fetch client to return full DTO, or just return basic info. 
        // Returning full DTO is better.
        var cliente = await _pedidoRepository.ObtenerClienteActivoPorRutAsync(pedido.Rut);
        
        return new PedidoResponseDto
        {
            IdPedido = pedido.IdPedido,
            NombreCliente = cliente != null ? cliente.Nombre + " " + cliente.Apellido : "Desconocido",
            Fecha = pedido.Fecha,
            Estado = pedido.Estado,
            Total = pedido.Total,
            Anotaciones = pedido.Anotaciones
        };
    }
}
