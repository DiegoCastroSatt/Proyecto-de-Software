using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Pedidos.Controllers;
using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestPedidosController
{
    [Fact]
    public async Task GetPedidoById_Devuelve404CuandoElPedidoNoExiste()
    {
        var controlador = new PedidosController(new ServicioPedidosFalso());

        var resultado = await controlador.GetPedidoById(404);

        var noEncontrado = Assert.IsType<NotFoundObjectResult>(resultado);
        Assert.Equal(404, noEncontrado.StatusCode);
    }

    [Fact]
    public async Task GetPedidoById_Devuelve200ConElDetalleEncontrado()
    {
        var esperado = new PedidoResponseDto
        {
            IdPedido = 12,
            NombreCliente = "Ana Pérez",
            Estado = "Listo",
            Total = 0
        };
        var servicio = new ServicioPedidosFalso { Pedido = esperado };

        var resultado = await new PedidosController(servicio).GetPedidoById(12);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task GetPedidos_Devuelve200AunqueNoHayaPedidos()
    {
        var resultado = await new PedidosController(new ServicioPedidosFalso()).GetPedidos();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<PedidoResponseDto>>(ok.Value));
    }

    [Fact]
    public async Task UpdateEstado_Devuelve404SiElPedidoNoExiste()
    {
        var servicio = new ServicioPedidosFalso
        {
            ErrorAlActualizar = new KeyNotFoundException("Pedido no encontrado.")
        };

        var resultado = await new PedidosController(servicio).UpdateEstado(
            404,
            new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task UpdateEstado_Devuelve400ParaEstadoInvalido()
    {
        var servicio = new ServicioPedidosFalso
        {
            ErrorAlActualizar = new ArgumentException("Estado no válido.")
        };

        var resultado = await new PedidosController(servicio).UpdateEstado(
            12,
            new UpdateEstadoPedidoDto { Estado = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task UpdateEstado_DevuelveAdvertenciaSiFallaLaNotificacion()
    {
        var servicio = new ServicioPedidosFalso
        {
            ResultadoActualizacion = new PedidoResponseDto
            {
                IdPedido = 12,
                Estado = "Listo",
                EstadoNotificacion = "Fallida"
            }
        };

        var resultado = await new PedidosController(servicio).UpdateEstado(
            12,
            new UpdateEstadoPedidoDto { Estado = "Listo" });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(
            "El pedido se actualizó correctamente, pero no se pudo enviar el correo al cliente.",
            ok.Value?.GetType().GetProperty("mensaje")?.GetValue(ok.Value));
    }

    private sealed class ServicioPedidosFalso : IPedidoService
    {
        public PedidoResponseDto? Pedido { get; set; }
        public PedidoResponseDto? ResultadoActualizacion { get; set; }
        public Exception? ErrorAlActualizar { get; set; }

        public Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosAsync()
            => Task.FromResult<IEnumerable<PedidoResponseDto>>([]);

        public Task<IEnumerable<ClienteActivoDto>> ObtenerClientesActivosAsync()
            => Task.FromResult<IEnumerable<ClienteActivoDto>>([]);

        public Task<PedidoResponseDto> CrearPedidoAsync(CreatePedidoDto dto)
            => Task.FromResult(new PedidoResponseDto { Estado = dto.Estado });

        public Task<PedidoResponseDto?> ObtenerPedidoPorIdAsync(int id)
            => Task.FromResult(Pedido);

        public Task<PedidoResponseDto> ActualizarEstadoAsync(int id, UpdateEstadoPedidoDto dto)
        {
            if (ErrorAlActualizar is not null)
                return Task.FromException<PedidoResponseDto>(ErrorAlActualizar);
            return Task.FromResult(ResultadoActualizacion ?? new PedidoResponseDto
            {
                IdPedido = id,
                Estado = dto.Estado
            });
        }
    }
}
