using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Pedidos.Repositories;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestPedidoRepository
{
    [Fact]
    public async Task ObtenerPedidosDetalle_OrdenaPorFechaYOmitePedidosSinCliente()
    {
        await using var contexto = CrearContexto();
        contexto.Clientes.Add(new Cliente
        {
            IdCliente = 1,
            Rut = "123456785",
            Nombre = "Ana",
            Apellido = "Pérez"
        });
        contexto.Pedidos.AddRange(
            new Pedido { IdPedido = 1, Rut = "123456785", Fecha = new DateTime(2026, 10, 1), Estado = "Pendiente", Total = 1000 },
            new Pedido { IdPedido = 2, Rut = "123456785", Fecha = new DateTime(2026, 10, 3), Estado = "Listo", Total = 0, Anotaciones = "Urgente" },
            new Pedido { IdPedido = 3, Rut = "999999999", Fecha = new DateTime(2026, 10, 4), Estado = "Pendiente", Total = 500 });
        await contexto.SaveChangesAsync();

        var pedidos = (await new PedidoRepository(contexto).ObtenerPedidosDetalleAsync()).ToArray();

        Assert.Collection(
            pedidos,
            pedido =>
            {
                Assert.Equal(2, pedido.IdPedido);
                Assert.Equal("Ana Pérez", pedido.NombreCliente);
                Assert.Equal(0, pedido.Total);
                Assert.Equal("Urgente", pedido.Anotaciones);
            },
            pedido =>
            {
                Assert.Equal(1, pedido.IdPedido);
                Assert.Equal("Pendiente", pedido.Estado);
            });
    }

    [Fact]
    public async Task ObtenerPedidoDetalle_DevuelveNullParaIdInexistenteOPedidoSinCliente()
    {
        await using var contexto = CrearContexto();
        contexto.Pedidos.Add(new Pedido
        {
            IdPedido = 12,
            Rut = "123456785",
            Fecha = new DateTime(2026, 10, 1),
            Estado = "Pendiente"
        });
        await contexto.SaveChangesAsync();

        var repositorio = new PedidoRepository(contexto);

        Assert.Null(await repositorio.ObtenerPedidoDetalleAsync(404));
        Assert.Null(await repositorio.ObtenerPedidoDetalleAsync(12));
    }

    [Fact]
    public async Task ObtenerPedidoDetalle_ProyectaDetalleDelPedidoYCliente()
    {
        await using var contexto = CrearContexto();
        contexto.Clientes.Add(new Cliente
        {
            IdCliente = 1,
            Rut = "123456785",
            Nombre = "Ana",
            Apellido = "Pérez"
        });
        contexto.Pedidos.Add(new Pedido
        {
            IdPedido = 12,
            Rut = "123456785",
            Fecha = new DateTime(2026, 10, 1),
            Estado = "En proceso",
            Total = 24500,
            Anotaciones = null
        });
        await contexto.SaveChangesAsync();

        var detalle = await new PedidoRepository(contexto).ObtenerPedidoDetalleAsync(12);

        Assert.NotNull(detalle);
        Assert.Equal(12, detalle.IdPedido);
        Assert.Equal("Ana Pérez", detalle.NombreCliente);
        Assert.Equal(new DateTime(2026, 10, 1), detalle.Fecha);
        Assert.Equal("En proceso", detalle.Estado);
        Assert.Equal(24500, detalle.Total);
        Assert.Null(detalle.Anotaciones);
    }

    [Fact]
    public async Task ObtenerClientesActivos_ExcluyeInactivosYOrdenaPorNombre()
    {
        await using var contexto = CrearContexto();
        contexto.Clientes.AddRange(
            new Cliente { IdCliente = 1, Rut = "111111111", Nombre = "Zoe", Apellido = "Z", Estado = "Activo" },
            new Cliente { IdCliente = 2, Rut = "222222222", Nombre = "Ana", Apellido = "A", Estado = "Activo" },
            new Cliente { IdCliente = 3, Rut = "333333333", Nombre = "Bea", Apellido = "B", Estado = "Inactivo" });
        await contexto.SaveChangesAsync();

        var clientes = (await new PedidoRepository(contexto).ObtenerClientesActivosAsync()).ToArray();

        Assert.Equal(new[] { "Ana", "Zoe" }, clientes.Select(cliente => cliente.Nombre));
        Assert.DoesNotContain(clientes, cliente => cliente.Estado == "Inactivo");
    }

    [Fact]
    public async Task ObtenerClienteActivoPorRut_NormalizaFormatoYRechazaInactivo()
    {
        await using var contexto = CrearContexto();
        contexto.Clientes.AddRange(
            new Cliente { IdCliente = 1, Rut = "12.345.678-5", Nombre = "Ana", Estado = "Activo" },
            new Cliente { IdCliente = 2, Rut = "98.765.432-1", Nombre = "Luis", Estado = "Inactivo" });
        await contexto.SaveChangesAsync();
        var repositorio = new PedidoRepository(contexto);

        var activo = await repositorio.ObtenerClienteActivoPorRutAsync("123456785");
        var inactivo = await repositorio.ObtenerClienteActivoPorRutAsync("987654321");

        Assert.Equal("Ana", activo?.Nombre);
        Assert.Null(inactivo);
    }

    [Fact]
    public async Task ActualizarPedido_PersisteCambioDeEstadoSinAlterarOtrosDatos()
    {
        await using var contexto = CrearContexto();
        var pedido = new Pedido
        {
            IdPedido = 12,
            Rut = "123456785",
            Fecha = new DateTime(2026, 10, 1),
            Estado = "En proceso",
            Total = 24500,
            Anotaciones = "Antireflejo"
        };
        contexto.Pedidos.Add(pedido);
        await contexto.SaveChangesAsync();

        pedido.Estado = "Listo";
        await new PedidoRepository(contexto).ActualizarPedidoAsync(pedido);
        contexto.ChangeTracker.Clear();

        var persistido = await contexto.Pedidos.SingleAsync(p => p.IdPedido == 12);
        Assert.Equal("Listo", persistido.Estado);
        Assert.Equal(24500, persistido.Total);
        Assert.Equal("Antireflejo", persistido.Anotaciones);
    }

    private static OpticaDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<OpticaDbContext>()
            .UseInMemoryDatabase($"pedidos-{Guid.NewGuid()}")
            .Options;
        return new OpticaDbContext(opciones);
    }
}
