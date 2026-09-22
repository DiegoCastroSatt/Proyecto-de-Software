using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Optica.Api.Modules.Productos.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestProductos
{
    [Fact]
    public async Task CrearProducto_RechazaCamposObligatoriosAusentes()
    {
        var repositorio = new ProductoRepositorioPrueba();
        var servicio = CrearServicio(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearProducto(new CrearProductoDto
        {
            Codigo = "",
            Nombre = "Armazón",
            Categoria = "Armazones"
        }));

        Assert.Null(repositorio.UltimoProducto);
    }

    [Fact]
    public async Task CrearProducto_RechazaCantidadesNegativas()
    {
        var servicio = CrearServicio(new ProductoRepositorioPrueba());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearProducto(new CrearProductoDto
        {
            Codigo = "OPT-001",
            Nombre = "Armazón",
            Categoria = "Armazones",
            Precio = 100,
            Stock = -1,
            StockMinimo = 0
        }));
    }

    [Fact]
    public async Task CrearProducto_RechazaCodigoDuplicado()
    {
        var repositorio = new ProductoRepositorioPrueba { CodigoExiste = true };
        var servicio = CrearServicio(repositorio);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.CrearProducto(ProductoValido()));
        Assert.Null(repositorio.UltimoProducto);
    }

    [Fact]
    public async Task CrearProducto_NormalizaDatosYDevuelveRespuesta()
    {
        var repositorio = new ProductoRepositorioPrueba();
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.CrearProducto(new CrearProductoDto
        {
            Codigo = "  OPT-001  ",
            Nombre = "  Armazón clásico  ",
            Marca = " Ray-Ban ",
            Modelo = " RB2140 ",
            Color = " Negro ",
            Categoria = " Armazones ",
            Precio = 45000,
            Stock = 5,
            StockMinimo = 1,
            Estado = "agotado"
        });

        Assert.Equal(1, resultado.Id);
        Assert.Equal("OPT-001", resultado.Codigo);
        Assert.Equal("Armazón clásico", resultado.Nombre);
        Assert.Equal("Agotado", resultado.Estado);
        Assert.Equal("OPT-001", repositorio.UltimoProducto!.Codigo);
        Assert.Equal("Ray-Ban", repositorio.UltimoProducto.Marca);
    }

    [Fact]
    public async Task EliminarProducto_ExplicaLaRestriccionPorVentas()
    {
        var repositorio = new ProductoRepositorioPrueba
        {
            ErrorEnEliminar = true,
            ProductoActual = new Producto { IdProducto = 7, Nombre = "Armazón", Codigo = "OPT-007", Categoria = "Armazones" }
        };
        var servicio = CrearServicio(repositorio);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EliminarProducto(7));

        Assert.Contains("ventas", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ProductoService CrearServicio(ProductoRepositorioPrueba repositorio) =>
        new(repositorio, new EntornoPrueba());

    private static CrearProductoDto ProductoValido() => new()
    {
        Codigo = "OPT-001",
        Nombre = "Armazón",
        Categoria = "Armazones",
        Precio = 100,
        Stock = 1,
        StockMinimo = 1,
        Estado = "Disponible"
    };
}

internal sealed class ProductoRepositorioPrueba : IProductoRepository
{
    public bool CodigoExiste { get; set; }
    public bool ErrorEnEliminar { get; set; }
    public Producto? ProductoActual { get; set; }
    public Producto? UltimoProducto { get; private set; }
    public List<Producto> Productos { get; } = [];

    public Task<bool> ExisteCodigo(string codigo) => Task.FromResult(CodigoExiste);

    public Task<Producto> Crear(Producto producto)
    {
        producto.IdProducto = 1;
        UltimoProducto = producto;
        return Task.FromResult(producto);
    }

    public Task<List<Producto>> Buscar(string termino) => Task.FromResult(Productos);

    public Task<Producto?> ObtenerPorId(int id) => Task.FromResult(ProductoActual ?? UltimoProducto);

    public Task<Producto> Actualizar(Producto producto)
    {
        UltimoProducto = producto;
        return Task.FromResult(producto);
    }

    public Task<bool> Eliminar(int id)
    {
        if (ErrorEnEliminar)
        {
            throw new DbUpdateException("No se puede eliminar el producto porque está asociado a ventas.");
        }

        UltimoProducto = null;
        return Task.FromResult(true);
    }
}

internal sealed class EntornoPrueba : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Optica.Tests";
    public string EnvironmentName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "optica-tests");
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
