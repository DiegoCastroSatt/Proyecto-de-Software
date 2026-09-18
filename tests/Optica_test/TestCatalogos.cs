using Optica.Api.Modules.Catalogos.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestCatalogos
{
    [Fact]
    public async Task Crear_NormalizaElTipoYElNombre()
    {
        var repositorio = new CatalogoRepositorioPrueba();
        var servicio = new CatalogoService(repositorio);

        var resultado = await servicio.Crear(new CrearCatalogoItemDto
        {
            Tipo = " marca ",
            Nombre = "  Oakley  "
        });

        Assert.Equal("Marca", resultado.Tipo);
        Assert.Equal("Oakley", resultado.Nombre);
        Assert.Equal("Marca", repositorio.UltimoCreado!.Tipo);
    }

    [Fact]
    public async Task Crear_DevuelveExistenteSinDuplicar()
    {
        var existente = new CatalogoItem { IdCatalogo = 4, Tipo = "Color", Nombre = "Negro" };
        var repositorio = new CatalogoRepositorioPrueba { Existente = existente };
        var servicio = new CatalogoService(repositorio);

        var resultado = await servicio.Crear(new CrearCatalogoItemDto
        {
            Tipo = "Color",
            Nombre = "Negro"
        });

        Assert.Equal(4, resultado.Id);
        Assert.Equal(0, repositorio.Creaciones);
    }

    [Fact]
    public async Task Crear_RechazaTipoInvalido()
    {
        var servicio = new CatalogoService(new CatalogoRepositorioPrueba());

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.Crear(new CrearCatalogoItemDto
        {
            Tipo = "Talla",
            Nombre = "Grande"
        }));
    }

    [Fact]
    public async Task ObtenerPorTipo_NormalizaTipoYMapeaResultados()
    {
        var repositorio = new CatalogoRepositorioPrueba
        {
            Items = [new CatalogoItem { IdCatalogo = 2, Tipo = "Categoria", Nombre = "Armazones" }]
        };
        var servicio = new CatalogoService(repositorio);

        var resultado = await servicio.ObtenerPorTipo(" categoria ");

        Assert.Single(resultado);
        Assert.Equal(2, resultado[0].Id);
        Assert.Equal("Categoria", repositorio.UltimoTipoConsultado);
    }
}

internal sealed class CatalogoRepositorioPrueba : ICatalogoRepository
{
    public CatalogoItem? Existente { get; set; }
    public CatalogoItem? UltimoCreado { get; private set; }
    public string? UltimoTipoConsultado { get; private set; }
    public int Creaciones { get; private set; }
    public IReadOnlyList<CatalogoItem> Items { get; set; } = [];

    public Task<IReadOnlyList<CatalogoItem>> ObtenerPorTipo(string tipo)
    {
        UltimoTipoConsultado = tipo;
        return Task.FromResult(Items);
    }

    public Task<CatalogoItem?> ObtenerPorNombre(string tipo, string nombre) => Task.FromResult(Existente);

    public Task<CatalogoItem> Crear(CatalogoItem item)
    {
        Creaciones++;
        item.IdCatalogo = 10;
        UltimoCreado = item;
        return Task.FromResult(item);
    }
}
