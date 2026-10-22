using Optica.Api.Modules.Rostros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Marcos;
using Optica.Api.Modules.Productos.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestMarcos
{
    private const string Svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 240 120\"><path d=\"M0 0L20 20\" fill=\"none\" stroke=\"black\"/></svg>";
    private static OpticaDbContext Contexto()
    {
        var db = new OpticaDbContext(new DbContextOptionsBuilder<OpticaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.TiposRostro.AddRange(new TipoRostro { Codigo = "ovalado", Nombre = "Ovalado" }, new TipoRostro { Codigo = "redondo", Nombre = "Redondo" });
        db.SaveChanges(); return db;
    }
    private static TipoMarco Marco(string codigo, string categoria, bool activo = true) =>
        new() { Codigo = codigo, Nombre = codigo, Categoria = categoria, Activo = activo, Svg = Svg, Rostros = "ovalado" };
    private static Producto Producto(string categoria, int stock, string estado = "Disponible") =>
        new() { Categoria = categoria, Stock = stock, Estado = estado };

    [Fact]
    public async Task Disponibles_SoloActivosConStockPositivoPorCategoria()
    {
        await using var db = Contexto();
        db.TiposMarco.AddRange(Marco("redondo", "Redondo"), Marco("cuadrado", "Cuadrado", false),
            Marco("aviador", "Aviador"), Marco("alto", "Rectangular alto"), Marco("nuevo", "Nuevo"));
        db.Productos.AddRange(Producto("Marcos Redondos", 3), Producto("Redondo", 2), Producto("Cuadrado", 9),
            Producto("Aviador", 0), Producto("Aviador", 4, "Agotado"), Producto("Rectangular", 8), Producto("Nuevo", 1));
        await db.SaveChangesAsync();
        var respuesta = await new MarcosController(db).Disponibles(default);
        var tipos = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>(respuesta.Result).Value);
        Assert.Equal(2, tipos.Count);
        Assert.Equal(5, tipos.Single(t => t.Codigo == "redondo").Stock);
        Assert.Contains(tipos, t => t.Codigo == "nuevo");
    }

    [Fact]
    public async Task Crear_GuardaSvgRostrosYCategoriaEnCatalogo()
    {
        await using var db = Contexto();
        db.Productos.Add(Producto("Nueva categoría", 6)); await db.SaveChangesAsync();
        var resultado = await new MarcosController(db).Crear(new(" Nuevo ", " Nueva categoría ", Svg, ["ovalado", "redondo"]), default);
        var creado = Assert.IsType<TipoMarcoResponse>(Assert.IsType<CreatedResult>(resultado).Value);
        Assert.True(creado.Activo);
        Assert.Equal(6, creado.Stock);
        Assert.Equal("Nuevo", creado.Nombre);
        Assert.Equal(["ovalado", "redondo"], creado.Rostros);
        Assert.Equal("Nueva categoría", (await db.Catalogos.SingleAsync()).Nombre);
        Assert.Contains("<path", (await db.TiposMarco.SingleAsync()).Svg);
    }

    [Fact]
    public async Task Crear_RechazaCategoriaEquivalenteSinDuplicar()
    {
        await using var db = Contexto();
        db.TiposMarco.Add(Marco("geometrico", "Geométrico", false)); await db.SaveChangesAsync();
        var resultado = await new MarcosController(db).Crear(new("Otro", "Marcos geometricos", Svg, ["ovalado"]), default);
        Assert.IsType<ConflictObjectResult>(resultado);
        Assert.Equal(1, await db.TiposMarco.CountAsync());
    }

    [Fact]
    public async Task DesactivarReactivarYBorrar_AfectaVistasPublicasSinBorrarProductos()
    {
        await using var db = Contexto();
        var marco = Marco("redondo", "Redondo"); db.TiposMarco.Add(marco);
        db.Productos.Add(Producto("Redondo", 4)); await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        Assert.IsType<NoContentResult>(await controller.CambiarEstado(marco.Id, new(false), default));
        var lista = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value);
        Assert.Empty(lista);
        Assert.IsType<NoContentResult>(await controller.CambiarEstado(marco.Id, new(true), default));
        Assert.IsType<NoContentResult>(await controller.Borrar(marco.Id, default));
        Assert.Empty(await db.TiposMarco.ToListAsync());
        Assert.Equal(4, (await db.Productos.SingleAsync()).Stock);
    }

    [Fact]
    public async Task Crear_RechazaRostrosInvalidos()
    {
        await using var db = Contexto();
        Assert.IsType<BadRequestObjectResult>(await new MarcosController(db).Crear(new("Nuevo", "Nuevo", Svg, ["invalido"]), default));
        Assert.Empty(await db.TiposMarco.ToListAsync());
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<image href='https://example.com/x'/>")]
    [InlineData("<path onload='alert(1)'/>")]
    [InlineData("<path fill='url(https://example.com/x)'/>")]
    [InlineData("<foreignObject/>")]
    public void Svg_RechazaContenidoActivoOExterno(string contenido)
    {
        Assert.Throws<ArgumentException>(() => ValidacionMarco.ValidarSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 240 120'>" + contenido + "</svg>"));
    }

    [Theory]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'/>")]
    [InlineData("<!DOCTYPE svg [<!ENTITY a 'test'>]><svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1 1'>&a;</svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 -1 0'/>")]
    public void Svg_RechazaXmlYDimensionesInvalidas(string svg) => Assert.Throws<ArgumentException>(() => ValidacionMarco.ValidarSvg(svg));

    [Fact]
    public void Mutaciones_NoPermitenAccesoAnonimo()
    {
        foreach (var metodo in new[] { "Crear", "CambiarEstado", "Borrar", "Listar", "ValidarDiseno" })
            Assert.Empty(typeof(MarcosController).GetMethod(metodo)!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        Assert.NotEmpty(typeof(MarcosController).GetMethod("Disponibles")!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }
    [Fact]
    public void Svg_AceptaElementosYAtributosDelDisenoSinListaFija()
    {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 240 120'><defs><linearGradient id='tono'><stop offset='0%' stop-color='red'/><stop offset='100%' stop-color='blue'/></linearGradient><clipPath id='recorte'><ellipse cx='100' cy='50' rx='30' ry='20'/></clipPath></defs><polygon id='lente' points='10,20 80,20 90,70' clip-path='url(#recorte)' fill='url(#tono)'/><use href='#lente'/></svg>";
        var resultado = ValidacionMarco.ValidarSvg(svg);
        Assert.Contains("linearGradient", resultado); Assert.Contains("clip-path", resultado);
        Assert.Contains("url(#tono)", resultado); Assert.Contains("<use", resultado);
    }

    [Fact]
    public void Svg_PreparaDimensionesYEstilosSinExigirEdicionManual()
    {
        var resultado = ValidacionMarco.ValidarSvg("<svg xmlns='http://www.w3.org/2000/svg' width='240px' height='120px'><path d='M0 0L20 20' style='fill:none;stroke:#ff0000;stroke-width:4'/></svg>");
        Assert.Contains("viewBox=\"0 0 240 120\"", resultado);
        Assert.Contains("stroke=\"#ff0000\"", resultado); Assert.DoesNotContain("style=", resultado);
    }

    [Theory]
    [InlineData("<use href='javascript:alert(1)'/>")]
    [InlineData("<path style='fill:url(https://example.com/x)'/>")]
    [InlineData("<path style='fill:u\\72l(https://example.com/x)'/>")]
    [InlineData("<animate attributeName='href' to='https://example.com/x'/>")]
    [InlineData("<path onpointerdown='alert(1)'/>")]
    [InlineData("<g xml:base='https://example.com/'><use href='#x'/></g>")]
    public void Svg_MantieneBloqueadosContenidoActivoYReferenciasExternas(string contenido)
    {
        Assert.Throws<ArgumentException>(() => ValidacionMarco.ValidarSvg("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 240 120'>" + contenido + "</svg>"));
    }

}
