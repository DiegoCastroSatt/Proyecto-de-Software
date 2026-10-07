using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Pedidos.Services;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestPedidos
{
    [Fact]
    public async Task Crear_NormalizaRutYPreservaDatos()
    {
        var repo = new RepositorioPedidos();
        var fecha = new DateTime(2026, 10, 1);
        var resultado = await new PedidoService(repo).CrearPedidoAsync(new CreatePedidoDto
        { Rut = " 12.345.678-5 ", Fecha = fecha, Total = 25000, Anotaciones = "Armazón negro" });
        Assert.Equal("123456785", repo.RutConsultado);
        Assert.Equal("123456785", repo.Guardado!.Rut);
        Assert.Equal(42, resultado.IdPedido);
        Assert.Equal("Ana Pérez", resultado.NombreCliente);
        Assert.Equal(fecha, resultado.Fecha);
        Assert.Equal(25000, resultado.Total);
        Assert.Equal("Pendiente", resultado.Estado);
        Assert.Equal("Armazón negro", resultado.Anotaciones);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12.345.678-9")]
    [InlineData("abc")]
    public async Task Crear_RechazaRutInvalidoSinConsultarNiGuardar(string rut)
    {
        var repo = new RepositorioPedidos();
        await Assert.ThrowsAsync<ArgumentException>(() => new PedidoService(repo).CrearPedidoAsync(new CreatePedidoDto { Rut = rut }));
        Assert.Null(repo.RutConsultado);
        Assert.Null(repo.Guardado);
    }

    [Fact]
    public async Task Crear_RechazaClienteNoActivoOInexistente()
    {
        var repo = new RepositorioPedidos { Cliente = null };
        await Assert.ThrowsAsync<ArgumentException>(() => new PedidoService(repo).CrearPedidoAsync(new CreatePedidoDto { Rut = "12345678-5" }));
        Assert.Null(repo.Guardado);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("En proceso")]
    [InlineData("Listo")]
    [InlineData("Entregado")]
    [InlineData("Cancelado")]
    public async Task Actualizar_AceptaEstadosValidosSinAlterarImporte(string estado)
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Total = 1000 } };
        var resultado = await new PedidoService(repo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = estado });
        Assert.Equal(1, repo.Actualizaciones);
        Assert.Equal(estado, repo.Pedido.Estado);
        Assert.Equal(estado, resultado.Estado);
        Assert.Equal(1000, resultado.Total);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Desconocido")]
    public async Task Actualizar_RechazaEstadoInvalidoSinModificar(string estado)
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { Estado = "Pendiente" } };
        await Assert.ThrowsAsync<ArgumentException>(() => new PedidoService(repo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = estado }));
        Assert.Equal(0, repo.Actualizaciones);
        Assert.Equal("Pendiente", repo.Pedido.Estado);
    }

    [Fact]
    public async Task Actualizar_RechazaPedidoInexistente()
    {
        var repo = new RepositorioPedidos();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new PedidoService(repo).ActualizarEstadoAsync(99, new UpdateEstadoPedidoDto { Estado = "Listo" }));
        Assert.Equal(0, repo.Actualizaciones);
    }

    private sealed class RepositorioPedidos : IPedidoRepository
    {
        public Cliente? Cliente { get; set; } = new() { Rut = "123456785", Nombre = "Ana", Apellido = "Pérez" };
        public Pedido? Pedido { get; set; }
        public Pedido? Guardado { get; private set; }
        public string? RutConsultado { get; private set; }
        public int Actualizaciones { get; private set; }
        public Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut) { RutConsultado = rut; return Task.FromResult(Cliente); }
        public Task<Pedido> CrearPedidoAsync(Pedido pedido) { Guardado = pedido; pedido.IdPedido = 42; return Task.FromResult(pedido); }
        public Task<Pedido?> ObtenerPedidoPorIdAsync(int id) => Task.FromResult(Pedido);
        public Task ActualizarPedidoAsync(Pedido pedido) { Actualizaciones++; return Task.CompletedTask; }
        public Task<IEnumerable<Pedido>> ObtenerPedidosAsync() => Task.FromResult<IEnumerable<Pedido>>([]);
        public Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync() => Task.FromResult<IEnumerable<PedidoResponseDto>>([]);
        public Task<PedidoResponseDto?> ObtenerPedidoDetalleAsync(int id) => Task.FromResult<PedidoResponseDto?>(null);
        public Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync() => Task.FromResult<IEnumerable<Cliente>>([]);
    }
}
