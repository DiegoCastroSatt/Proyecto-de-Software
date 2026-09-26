using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Ventas.Controllers;
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
        var servicio = CrearServicio(out var repositorio);
        salida.WriteLine("PREPARACIÓN: productos A x2, B x1 y A x3.");

        var venta = await servicio.Crear(Solicitud(("A", 2), ("B", 1), ("A", 3)));
        salida.WriteLine("RESULTADO: {0} productos agrupados y total ${1}.", venta.Productos.Count, venta.Total);

        Assert.Equal(750, venta.Total);
        Assert.Equal(2, venta.Productos.Count);
        Assert.Equal(5, venta.Productos.Single(p => p.ProductoId == 1).Cantidad);
        Assert.Equal("Producto A", repositorio.UltimaVenta!.Detalles.Single(p => p.ProductoId == 1).NombreProducto);
        salida.WriteLine("OK: A quedó con 5 unidades, su nombre quedó en el historial y el total es $750.");
    }

    [Fact]
    public async Task Crear_UsaLaHoraDeSantiagoAunqueElServidorEsteEnUtc()
    {
        var repositorio = new RepositorioPrueba();
        var utc = new DateTimeOffset(2026, 9, 25, 17, 30, 0, TimeSpan.Zero);
        var servicio = new VentaService(repositorio, new ConsultaPrueba(), new CalculoVenta(), new RelojPrueba(utc));

        var venta = await servicio.Crear(Solicitud(("A", 1)));

        Assert.Equal(new DateTime(2026, 9, 25, 14, 30, 0), venta.Fecha);
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

    [Fact]
    public async Task Api_Listar_DevuelveFechasYUnidadesParaCalcularElRank()
    {
        salida.WriteLine("INICIO: comprobar los datos del historial utilizados por el Rank.");
        var servicio = CrearServicio(out var repositorio);
        var fecha = new DateTime(2026, 9, 24, 12, 0, 0);
        repositorio.Historial = [
            new VentaResponseDto(1, fecha, 700, [new ProductoHistorialDto(2, "Producto B", 7, 100, 700)]),
            new VentaResponseDto(2, fecha.AddDays(-15), 200, [new ProductoHistorialDto(1, "Producto A", 2, 100, 200)])
        ];
        var controlador = new VentasController(servicio, new ConsultaPrueba());

        var respuesta = Assert.IsType<OkObjectResult>(await controlador.Listar());
        var historial = Assert.IsAssignableFrom<IReadOnlyList<VentaResponseDto>>(respuesta.Value);

        Assert.Equal(200, respuesta.StatusCode);
        Assert.Collection(historial,
            venta => {
                Assert.Equal(1, venta.IdVenta);
                Assert.Equal(fecha, venta.Fecha);
                Assert.Equal(700m, venta.Total);
                Assert.Equal(new ProductoHistorialDto(2, "Producto B", 7, 100, 700), Assert.Single(venta.Productos));
            },
            venta => {
                Assert.Equal(fecha.AddDays(-15), venta.Fecha);
                Assert.Equal(new ProductoHistorialDto(1, "Producto A", 2, 100, 200), Assert.Single(venta.Productos));
            });
        salida.WriteLine("OK: la API conserva fechas, identificadores y cantidades del historial.");
    }

    [Fact]
    public async Task Api_Listar_SinVentasDevuelveListaVacia()
    {
        var controlador = new VentasController(CrearServicio(out _), new ConsultaPrueba());
        var respuesta = Assert.IsType<OkObjectResult>(await controlador.Listar());
        Assert.Equal(200, respuesta.StatusCode);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<VentaResponseDto>>(respuesta.Value));
    }

    [Fact]
    public async Task Api_BuscarProducto_DevuelveProductoExistente()
    {
        var controlador = new VentasController(CrearServicio(out _), new ConsultaPrueba());
        var respuesta = Assert.IsType<OkObjectResult>(await controlador.BuscarProducto("A"));
        Assert.Equal(200, respuesta.StatusCode);
        Assert.Equal(new ProductoCajaDto(1, "A", "Producto A", 100),
            Assert.IsType<ProductoCajaDto>(respuesta.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Api_BuscarProducto_CodigoVacioDevuelve400(string codigo)
    {
        var controlador = new VentasController(CrearServicio(out _), new ConsultaPrueba());
        var respuesta = Assert.IsType<BadRequestObjectResult>(await controlador.BuscarProducto(codigo));
        Assert.Equal(400, respuesta.StatusCode);
        Assert.Equal("Ingresa un código.", Assert.IsType<ErrorVentaDto>(respuesta.Value).Mensaje);
    }

    [Fact]
    public async Task Api_BuscarProducto_InexistenteDevuelve404()
    {
        var controlador = new VentasController(CrearServicio(out _), new ConsultaPrueba());
        var respuesta = Assert.IsType<NotFoundObjectResult>(await controlador.BuscarProducto("X"));
        Assert.Equal(404, respuesta.StatusCode);
        Assert.Equal("No se encontró un producto con ese código.",
            Assert.IsType<ErrorVentaDto>(respuesta.Value).Mensaje);
    }

    [Fact]
    public async Task Api_Crear_VentaValidaDevuelve201ConDetallesCalculados()
    {
        var servicio = CrearServicio(out var repositorio);
        var controlador = new VentasController(servicio, new ConsultaPrueba());
        var respuesta = Assert.IsType<CreatedAtActionResult>(
            await controlador.Crear(Solicitud(("A", 2), ("B", 1), ("A", 3))));
        var venta = Assert.IsType<VentaCreadaResponseDto>(respuesta.Value);

        Assert.Equal(201, respuesta.StatusCode);
        Assert.Equal(nameof(VentasController.Listar), respuesta.ActionName);
        Assert.Equal(1, venta.IdVenta);
        Assert.Equal(750m, venta.Total);
        Assert.Equal(2, venta.Productos.Count);
        Assert.Equal(new DetalleVentaResponseDto(1, 5, 100, 500),
            venta.Productos.Single(p => p.ProductoId == 1));
        Assert.Equal(new DetalleVentaResponseDto(2, 1, 250, 250),
            venta.Productos.Single(p => p.ProductoId == 2));
        Assert.Equal(1, repositorio.Guardadas);
    }

    [Theory]
    [InlineData("A", 0)]
    [InlineData("A", -1)]
    [InlineData("", 1)]
    [InlineData("X", 1)]
    public async Task Api_Crear_VentaInvalidaDevuelve400SinGuardar(string codigo, int cantidad)
    {
        var servicio = CrearServicio(out var repositorio);
        var controlador = new VentasController(servicio, new ConsultaPrueba());
        var respuesta = Assert.IsType<BadRequestObjectResult>(
            await controlador.Crear(Solicitud((codigo, cantidad))));

        Assert.Equal(400, respuesta.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<ErrorVentaDto>(respuesta.Value).Mensaje));
        Assert.Equal(0, repositorio.Guardadas);
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
    public Venta? UltimaVenta { get; private set; }
    public IReadOnlyList<VentaResponseDto> Historial { get; set; } = [];
    public Task Guardar(Venta venta) { UltimaVenta = venta; venta.IdVenta = ++Guardadas; return Task.CompletedTask; }
    public Task<IReadOnlyList<VentaResponseDto>> Listar() => Task.FromResult(Historial);
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

public sealed class RelojPrueba(DateTimeOffset fechaUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => fechaUtc;
}
