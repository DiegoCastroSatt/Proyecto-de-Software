using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Ventas.Services;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestVentas
{
    [Fact]
    public async Task Crear_AgrupaProductosRepetidosYCalculaElTotal()
    {
        var servicio = CrearServicio(out _);

        var venta = await servicio.Crear(Solicitud(("A", 2), ("B", 1), ("A", 3)));

        Assert.Equal(750, venta.Total);
        Assert.Equal(2, venta.Productos.Count);
        Assert.Equal(5, venta.Productos.Single(p => p.ProductoId == 1).Cantidad);
    }

    [Fact]
    public async Task Crear_PersisteLaVentaYDevuelveSuIdentificador()
    {
        var servicio = CrearServicio(out var repositorio);

        var venta = await servicio.Crear(Solicitud(("A", 1)));

        Assert.Equal(1, venta.IdVenta);
        Assert.Equal(1, repositorio.Guardadas);
    }

    [Fact]
    public async Task Crear_RechazaSolicitudesInvalidasSinGuardar()
    {
        var servicio = CrearServicio(out var repositorio);
        var solicitudesInvalidas = new[]
        {
            Solicitud(), Solicitud(("A", 0)), Solicitud(("A", 1), ("X", 1)),
            Solicitud(("A", int.MaxValue), ("A", 1)), Solicitud(("B", 1000000))
        };

        foreach (var solicitud in solicitudesInvalidas)
            await Assert.ThrowsAsync<ArgumentException>(() => servicio.Crear(solicitud));

        Assert.Equal(0, repositorio.Guardadas);
    }

    [Fact]
    public async Task Crear_PermiteSustituirLaReglaDeCalculo()
    {
        var repositorio = new RepositorioPrueba();
        var servicio = new VentaService(repositorio, new ConsultaPrueba(), new CalculoAlternativo(), TimeProvider.System);

        var venta = await servicio.Crear(Solicitud(("A", 1)));

        Assert.Equal(50, venta.Total);
    }

    private static VentaService CrearServicio(out RepositorioPrueba repositorio)
    {
        repositorio = new RepositorioPrueba();
        return new VentaService(repositorio, new ConsultaPrueba(), new CalculoVenta(), TimeProvider.System);
    }

    private static CrearVentaDto Solicitud(params (string Codigo, int Cantidad)[] items) => new()
    {
        Productos = items.Select(i => new ProductoVentaDto
            { CodigoProducto = i.Codigo, Cantidad = i.Cantidad }).ToList()
    };
}

public sealed class RepositorioPrueba : IVentaRepository
{
    public int Guardadas { get; private set; }
    public Task Guardar(Venta venta) { venta.IdVenta = ++Guardadas; return Task.CompletedTask; }
    public Task<IReadOnlyList<VentaResponseDto>> Listar() => Task.FromResult<IReadOnlyList<VentaResponseDto>>([]);
}

public sealed class ConsultaPrueba : IConsultaProductoVenta
{
    public Task<ProductoCajaDto?> Buscar(string codigo) => Task.FromResult(codigo switch
    {
        "A" => new ProductoCajaDto(1, "A", "Producto A", 100),
        "B" => new ProductoCajaDto(2, "B", "Producto B", 250),
        _ => null
    });
}

public sealed class CalculoAlternativo : ICalculoVenta
{
    public void Calcular(Venta venta)
    {
        foreach (var detalle in venta.Detalles)
            detalle.Subtotal = detalle.PrecioUnitario * detalle.Cantidad / 2;
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);
    }
}
