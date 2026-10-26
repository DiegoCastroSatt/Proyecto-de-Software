using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Clientes.Services;
using System.Net.Mail;
using Optica.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Optica.Api.Modules.Pedidos.Services;

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IPedidoCorreoService _correoService;
    private readonly OpticaDbContext? _dbContext;

    public PedidoService(IPedidoRepository pedidoRepository, IPedidoCorreoService correoService, OpticaDbContext? dbContext = null)
    {
        _pedidoRepository = pedidoRepository;
        _correoService = correoService;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosAsync()
    {
        return await _pedidoRepository.ObtenerPedidosDetalleAsync();
    }

    public async Task<PedidoResponseDto?> ObtenerPedidoPorIdAsync(int id)
    {
        return await _pedidoRepository.ObtenerPedidoDetalleAsync(id);
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

    public async Task<IEnumerable<ClienteActivoDto>> BuscarClientesPorRutAsync(string rut)
    {
        var rutNormalizado = RutChilenoValidator.Normalizar(rut);
        if (rutNormalizado.Length < 2 || rutNormalizado.Length > 9 ||
            !rutNormalizado.All(char.IsAsciiLetterOrDigit))
        {
            return [];
        }

        var clientes = await _pedidoRepository.BuscarClientesPorRutAsync(rutNormalizado);
        return clientes.Select(c => new ClienteActivoDto
        {
            Rut = c.Rut,
            NombreCompleto = c.Nombre + " " + c.Apellido
        });
    }

    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosPorRutAsync(string rut)
    {
        if (!RutChilenoValidator.EsValido(rut))
        {
            throw new ArgumentException("El RUT o su dígito verificador no es válido.");
        }

        var rutNormalizado = RutChilenoValidator.Normalizar(rut);
        var cliente = await _pedidoRepository.ObtenerClientePorRutAsync(rutNormalizado);
        if (cliente == null)
        {
            throw new KeyNotFoundException("No se encontró un cliente con ese RUT.");
        }

        return await _pedidoRepository.ObtenerPedidosDetallePorRutAsync(rutNormalizado);
    }

    public async Task<PedidoResponseDto> CrearPedidoAsync(CreatePedidoDto dto)
    {
        if (!RutChilenoValidator.EsValido(dto.Rut))
        {
            throw new ArgumentException("El RUT o su dígito verificador no es válido.");
        }

        var rut = RutChilenoValidator.Normalizar(dto.Rut);
        var cliente = await _pedidoRepository.ObtenerClienteActivoPorRutAsync(rut);
        if (cliente == null)
        {
            throw new ArgumentException("No se encontró un cliente activo con ese RUT.");
        }

        var pedido = new Pedido
        {
            Rut = cliente.Rut,
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
        await using var transaccion = _dbContext is null ? null : await _dbContext.Database.BeginTransactionAsync();
        var pedido = _dbContext is null
            ? await _pedidoRepository.ObtenerPedidoPorIdAsync(id)
            : await _pedidoRepository.ObtenerPedidoPorIdParaActualizarAsync(id);
        if (pedido == null)
        {
            throw new KeyNotFoundException("Pedido no encontrado.");
        }

        var estadosValidos = new[] { "Pendiente", "En proceso", "Listo", "Entregado", "Cancelado" };
        if (!estadosValidos.Contains(dto.Estado))
        {
            throw new ArgumentException("Estado no válido.");
        }

        var estabaListo = pedido.Estado == "Listo";
        pedido.Estado = dto.Estado;
        string? estadoNotificacion = null;
        if (dto.Estado == "Listo" && !estabaListo)
        {
            if (_dbContext is not null)
            {
                var notificacionExistente = await _dbContext.CorreosPedido
                    .Where(c => c.PedidoId == pedido.IdPedido)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();
                if (notificacionExistente is not null)
                {
                    estadoNotificacion = notificacionExistente.Enviado ? "YaEnviada" : "Pendiente";
                }
                else
                {
                    var clienteCorreo = await _pedidoRepository.ObtenerClientePorRutAsync(pedido.Rut);
                    var correo = _correoService.PrepararPedidoListo(
                        clienteCorreo?.Correo,
                        clienteCorreo is null ? string.Empty : $"{clienteCorreo.Nombre} {clienteCorreo.Apellido}".Trim(),
                        pedido.IdPedido);
                    _dbContext.CorreosPedido.Add(correo);
                    estadoNotificacion = "Pendiente";
                }
            }
            else
            {
                estadoNotificacion = await EnviarNotificacionAsync(pedido);
            }
        }
        await _pedidoRepository.ActualizarPedidoAsync(pedido);
        if (transaccion is not null)
            await transaccion.CommitAsync();

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
            Anotaciones = pedido.Anotaciones,
            EstadoNotificacion = estadoNotificacion
        };
    }

    private async Task<string> EnviarNotificacionAsync(Pedido pedido)
    {
        try
        {
            var cliente = await _pedidoRepository.ObtenerClientePorRutAsync(pedido.Rut);
            if (cliente?.Correo is null || !MailAddress.TryCreate(cliente.Correo, out _))
                return "Fallida";

            return await _correoService.EnviarPedidoListoAsync(cliente.Correo, $"{cliente.Nombre} {cliente.Apellido}".Trim(), pedido.IdPedido, CancellationToken.None)
                ? "Enviada"
                : "Fallida";
        }
        catch (Exception)
        {
            // El pedido ya quedó actualizado; un fallo de notificación no revierte su estado.
            return "Fallida";
        }
    }
}
