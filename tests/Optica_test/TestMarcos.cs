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
    public async Task Disponibles_SoloActivosConStockDeProductosVinculados()
    {
        await using var db = Contexto();
        var redondo = Marco("redondo", "Redondo"); var cuadrado = Marco("cuadrado", "Cuadrado", false);
        var aviador = Marco("aviador", "Aviador"); var nuevo = Marco("nuevo", "Armazones");
        db.TiposMarco.AddRange(redondo, cuadrado, aviador, Marco("alto", "Rectangular alto"), nuevo);
        foreach (var item in new[] { (redondo.Id, 3, "Disponible"), (redondo.Id, 2, "Disponible"), (cuadrado.Id, 9, "Disponible"),
            (aviador.Id, 0, "Disponible"), (aviador.Id, 4, "Agotado"), (nuevo.Id, 1, "Disponible") })
        {
            var producto = Producto("Armazones", item.Item2, item.Item3); producto.IdTipoMarco = item.Id; db.Productos.Add(producto);
        }
        db.Productos.Add(Producto("Armazones", 8));
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
        Assert.Equal(0, creado.Stock);
        Assert.Equal("Nuevo", creado.Nombre);
        Assert.Equal(["ovalado", "redondo"], creado.Rostros);
        Assert.Equal("Nueva categoría", (await db.Catalogos.SingleAsync()).Nombre);
        Assert.Contains("<path", (await db.TiposMarco.SingleAsync()).Svg);
    }

    [Fact]
    public async Task Crear_RechazaNombreEquivalenteSinDuplicar()
    {
        await using var db = Contexto();
        db.TiposMarco.Add(Marco("geometrico", "Geométrico", false)); await db.SaveChangesAsync();
        var resultado = await new MarcosController(db).Crear(new("Geométrico", "Armazones", Svg, ["ovalado"]), default);
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
        foreach (var metodo in new[] { "Crear", "CambiarEstado", "Borrar", "Listar", "ValidarDiseno", "VincularProductos", "ProductosVinculables" })
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

    [Fact]
    public async Task Vincular_UsaElStockDelArmazonSinCambiarCategoriaNiCantidad()
    {
        await using var db = Contexto(); var marco = Marco("redondo", "Redondo"); db.TiposMarco.Add(marco);
        var producto = Producto("Armazones", 7); db.Productos.Add(producto); await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        Assert.IsType<OkObjectResult>(await controller.VincularProductos(marco.Id, new([producto.IdProducto]), default));
        Assert.Equal(marco.Id, producto.IdTipoMarco); Assert.Equal("Armazones", producto.Categoria); Assert.Equal(7, producto.Stock);
        var disponibles = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value);
        Assert.Equal(7, Assert.Single(disponibles).Stock); Assert.Equal(1, disponibles[0].ProductosVinculados);
        producto.Stock = 0; await db.SaveChangesAsync();
        Assert.Empty(Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value));
    }

    [Fact]
    public async Task Vincular_ReasignaSinContarElProductoEnDosMarcosYPermiteDesvincular()
    {
        await using var db = Contexto(); var primero = Marco("primero", "Redondo"); var segundo = Marco("segundo", "Redondo");
        db.TiposMarco.AddRange(primero, segundo); var producto = Producto("Armazones", 5); db.Productos.Add(producto); await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        await controller.VincularProductos(primero.Id, new([producto.IdProducto]), default);
        await controller.VincularProductos(segundo.Id, new([producto.IdProducto]), default);
        var lista = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Listar(default)).Result).Value);
        Assert.Equal(0, lista.Single(m => m.Id == primero.Id).Stock); Assert.Equal(5, lista.Single(m => m.Id == segundo.Id).Stock);
        await controller.VincularProductos(segundo.Id, new([]), default); Assert.Null(producto.IdTipoMarco);
        Assert.Empty(Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value));
        Assert.Equal(5, producto.Stock);
    }

    [Fact]
    public async Task Vincular_RechazaProductosInexistentesSinDesvincularLosActuales()
    {
        await using var db = Contexto(); var marco = Marco("redondo", "Redondo"); db.TiposMarco.Add(marco);
        var producto = Producto("Armazones", 3); producto.IdTipoMarco = marco.Id; db.Productos.Add(producto); await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await new MarcosController(db).VincularProductos(marco.Id, new([9999]), default));
        Assert.Equal(marco.Id, producto.IdTipoMarco);
    }

    [Fact]
    public async Task BorrarMarco_DesvinculaArmazonesSinBorrarProductosNiStock()
    {
        await using var db = Contexto(); var marco = Marco("redondo", "Redondo"); db.TiposMarco.Add(marco);
        var producto = Producto("Armazones", 3); producto.IdTipoMarco = marco.Id; db.Productos.Add(producto); await db.SaveChangesAsync();
        await new MarcosController(db).Borrar(marco.Id, default);
        Assert.Null(producto.IdTipoMarco); Assert.Equal(3, (await db.Productos.SingleAsync()).Stock);
    }

    [Fact]
    public async Task Crear_PermiteVariosDisenosParaLaMismaCategoriaArmazones()
    {
        await using var db = Contexto(); var controller = new MarcosController(db);
        Assert.IsType<CreatedResult>(await controller.Crear(new("Redondo", "Armazones", Svg, ["ovalado"]), default));
        Assert.IsType<CreatedResult>(await controller.Crear(new("Cuadrado", "Armazones", Svg, ["ovalado"]), default));
    }

    [Theory]
    [InlineData("Cordones")]
    [InlineData("Accesorios")]
    [InlineData("Cordones para marcos")]
    [InlineData("Lentes de contacto")]
    [InlineData("Lentes de sol")]
    [InlineData("Lentes ópticos")]
    [InlineData("Marcos redondos")]
    [InlineData("Monturas")]
    public async Task Vincular_RechazaAccesoriosAunqueSuNombreMencioneMarcos(string categoria)
    {
        await using var db = Contexto(); var marco = Marco("redondo", "Armazones"); db.TiposMarco.Add(marco);
        var armazon = Producto("Armazones", 2); armazon.IdTipoMarco = marco.Id;
        var accesorio = Producto(categoria, 100); db.Productos.AddRange(armazon, accesorio); await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        Assert.IsType<BadRequestObjectResult>(await controller.VincularProductos(marco.Id, new([accesorio.IdProducto]), default));
        Assert.Null(accesorio.IdTipoMarco); Assert.Equal(marco.Id, armazon.IdTipoMarco);
        var vinculables = Assert.IsType<List<ProductoVinculableMarcoResponse>>(Assert.IsType<OkObjectResult>(await controller.ProductosVinculables(default)).Value);
        Assert.Equal(armazon.IdProducto, Assert.Single(vinculables).Id);
    }

    [Fact]
    public async Task Disponibles_NoSumaStockDeAccesoriosVinculadosAntesDeLaCorreccion()
    {
        await using var db = Contexto(); var marco = Marco("redondo", "Armazones"); db.TiposMarco.Add(marco);
        var accesorio = Producto("Accesorios", 100); accesorio.IdTipoMarco = marco.Id; db.Productos.Add(accesorio); await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        var publicos = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value);
        Assert.Empty(publicos);
    }

    [Theory]
    [InlineData("Armazones")]
    [InlineData(" ARMAZÓN ")]
    public void Clasificar_ReconoceCategoriasDeArmazones(string categoria) => Assert.True(ValidacionMarco.EsArmazon(categoria));

    [Fact]
    public async Task ListarFormas_DevuelveLaGeometriaYProporcionGuardadasSinDependerDelNombre()
    {
        await using var db = Contexto();
        db.FormasLente.Add(new FormaLente { Codigo = "modelo-nuevo", Nombre = "Modelo nuevo", NombreNormalizado = "modelo nuevo", Geometria = "elipse", MantenerProporcion = true });
        await db.SaveChangesAsync();
        var lista = Assert.IsType<List<FormaLenteResponse>>(Assert.IsType<OkObjectResult>(await new MarcosController(db).ListarFormas(default)).Value);
        var forma = Assert.Single(lista);
        Assert.Equal("elipse", forma.Geometria); Assert.True(forma.MantenerProporcion);
    }
    [Fact]
    public void ListarFormas_RequiereSesion()
    {
        Assert.Empty(typeof(MarcosController).GetMethod("ListarFormas")!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }
}
