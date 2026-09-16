using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Ventas.Services;
using Xunit;
using Xunit.Abstractions;

namespace Optica.Ventas.Tests;

public class TestVentas
{
    private readonly ITestOutputHelper salida;

    public TestVentas(ITestOutputHelper salida) => this.salida = salida;

    [Fact]
    public async Task Crear_AgrupaProductosRepetidosYCalculaElTotal()
    {
        salida.WriteLine("INICIO: comprobar agrupación de productos y cálculo del total.");
        var servicio = CrearServicio(out _);
        salida.WriteLine("PREPARACIÓN: productos A x2, B x1 y A x3.");

        var venta = await servicio.Crear(Solicitud(("A", 2), ("B", 1), ("A", 3)));
        salida.WriteLine("RESULTADO: {0} productos agrupados y total ${1}.", venta.Productos.Count, venta.Total);

        Assert.Equal(750, venta.Total);
        Assert.Equal(2, venta.Productos.Count);
        Assert.Equal(5, venta.Productos.Single(p => p.ProductoId == 1).Cantidad);
        salida.WriteLine("OK: A quedó con 5 unidades y el total esperado es $750.");
    }

    [Fact]
    public async Task Crear_PersisteLaVentaYDevuelveSuIdentificador()
    {
        salida.WriteLine("INICIO: comprobar persistencia e identificador de venta.");
        var servicio = CrearServicio(out var repositorio);
        salida.WriteLine("ACCIÓN: registrar una venta del producto A.");

        var venta = await servicio.Crear(Solicitud(("A", 1)));
        salida.WriteLine("RESULTADO: ID {0}; ventas guardadas: {1}.", venta.IdVenta, repositorio.Guardadas);

        Assert.Equal(1, venta.IdVenta);
        Assert.Equal(1, repositorio.Guardadas);
        salida.WriteLine("OK: la venta se persistió una sola vez y devolvió su ID.");
    }

    [Fact]
    public async Task Crear_RechazaSolicitudesInvalidasSinGuardar()
    {
        salida.WriteLine("INICIO: comprobar el rechazo de solicitudes inválidas.");
        var servicio = CrearServicio(out var repositorio);
        var solicitudesInvalidas = new[]
        {
            Solicitud(), Solicitud(("A", 0)), Solicitud(("A", 1), ("X", 1)),
            Solicitud(("A", int.MaxValue), ("A", 1)), Solicitud(("B", 1000000))
        };

        for (var indice = 0; indice < solicitudesInvalidas.Length; indice++)
        {
            salida.WriteLine("ACCIÓN: validar solicitud inválida {0} de {1}.", indice + 1, solicitudesInvalidas.Length);
            var solicitud = solicitudesInvalidas[indice];
            await Assert.ThrowsAsync<ArgumentException>(() => servicio.Crear(solicitud));
            salida.WriteLine("RESULTADO: solicitud {0} rechazada correctamente.", indice + 1);
        }

        Assert.Equal(0, repositorio.Guardadas);
        salida.WriteLine("OK: ninguna solicitud inválida fue guardada.");
    }

    [Fact]
    public async Task Crear_PermiteSustituirLaReglaDeCalculo()
    {
        salida.WriteLine("INICIO: comprobar sustitución de la regla de cálculo.");
        var repositorio = new RepositorioPrueba();
        var servicio = new VentaService(repositorio, new ConsultaPrueba(), new CalculoAlternativo(), TimeProvider.System);
        salida.WriteLine("ACCIÓN: crear una venta de $100 con cálculo alternativo del 50%.");

        var venta = await servicio.Crear(Solicitud(("A", 1)));
        salida.WriteLine("RESULTADO: total calculado ${0}.", venta.Total);

        Assert.Equal(50, venta.Total);
        salida.WriteLine("OK: el servicio utilizó la regla alternativa y obtuvo $50.");
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
