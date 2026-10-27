using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Marcos;
using Optica.Api.Modules.Rostros;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestRostros
{
    private static OpticaDbContext Contexto() => new(new DbContextOptionsBuilder<OpticaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static TipoMarco Marco(string codigo, string rostros) => new() { Codigo = codigo, Nombre = codigo, Categoria = codigo, Rostros = rostros };
    private static IFormFile Imagen() => new FormFile(new MemoryStream(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9ioAAAAASUVORK5CYII=")), 0, 68, "imagen", "rostro.png");
    private static CrearTipoRostroRequest Solicitud(params int[] marcos) => new() { Nombre = "Triangular", Rasgos = "Frente estrecha y mandíbula ancha", Consejo = "Prueba marcos con detalles arriba", Imagen = Imagen(), Marcos = marcos };

    [Fact]
    public async Task Crear_GuardaImagenYAsociacionesSinLimitarseALosSeisOriginales()
    {
        await using var db = Contexto(); var marco = Marco("redondo", "ovalado"); db.TiposMarco.Add(marco); await db.SaveChangesAsync();
        var respuesta = await new RostrosController(db).Crear(Solicitud(marco.Id), default);
        var creado = Assert.IsType<TipoRostroResponse>(Assert.IsType<CreatedResult>(respuesta).Value);
        Assert.Equal("Triangular", creado.Nombre); Assert.True(creado.Activo);
        var rostro = await db.TiposRostro.SingleAsync(); Assert.Equal("image/png", rostro.TipoImagen); Assert.NotEmpty(rostro.Imagen!);
        Assert.Equal(new[] { "ovalado", creado.Codigo }, marco.Rostros.Split(','));
        Assert.Equal(new[] { marco.Id }, creado.Marcos);
    }

    [Fact]
    public async Task DesactivarYReactivar_ConservaAsociacionesYFiltraLaListaPublica()
    {
        await using var db = Contexto(); var rostro = new TipoRostro { Codigo = "triangular", Nombre = "Triangular" };
        db.TiposRostro.Add(rostro); db.TiposMarco.Add(Marco("redondo", "triangular")); await db.SaveChangesAsync();
        var controller = new RostrosController(db);
        await controller.CambiarEstado(rostro.Id, new(false), default);
        Assert.Empty(Assert.IsType<List<TipoRostroResponse>>(Assert.IsType<OkObjectResult>(await controller.Activos(default)).Value));
        Assert.Equal("triangular", (await db.TiposMarco.SingleAsync()).Rostros);
        await controller.CambiarEstado(rostro.Id, new(true), default);
        Assert.Single(Assert.IsType<List<TipoRostroResponse>>(Assert.IsType<OkObjectResult>(await controller.Activos(default)).Value));
    }

    [Fact]
    public async Task Borrar_RetiraSoloSusAsociacionesSinBorrarMarcos()
    {
        await using var db = Contexto(); var rostro = new TipoRostro { Codigo = "redondo", Nombre = "Redondo" };
        db.TiposRostro.Add(rostro); db.TiposMarco.Add(Marco("marco", "redondo,ovalado")); await db.SaveChangesAsync();
        Assert.IsType<NoContentResult>(await new RostrosController(db).Borrar(rostro.Id, default));
        Assert.Empty(await db.TiposRostro.ToListAsync()); Assert.Equal("ovalado", (await db.TiposMarco.SingleAsync()).Rostros);
    }

    [Fact]
    public async Task Asociar_PermiteAgregarYRetirarMarcosSinAfectarOtrosRostros()
    {
        await using var db = Contexto(); var rostro = new TipoRostro { Codigo = "nuevo", Nombre = "Nuevo" };
        var primero = Marco("primero", "nuevo,ovalado"); var segundo = Marco("segundo", "ovalado");
        db.TiposRostro.Add(rostro); db.TiposMarco.AddRange(primero, segundo); await db.SaveChangesAsync();
        Assert.IsType<NoContentResult>(await new RostrosController(db).AsociarMarcos(rostro.Id, new([segundo.Id]), default));
        Assert.Equal("ovalado", primero.Rostros); Assert.Equal("ovalado,nuevo", segundo.Rostros);
    }

    [Fact]
    public async Task Crear_RechazaDuplicadoNormalizadoEImagenFalsaSinGuardarNada()
    {
        await using var db = Contexto(); db.TiposRostro.Add(new() { Codigo = "triangular", Nombre = "Triangular" }); await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await new RostrosController(db).Crear(Solicitud(), default));
        db.TiposRostro.RemoveRange(db.TiposRostro); await db.SaveChangesAsync();
        var solicitud = Solicitud(); solicitud.Imagen = new FormFile(new MemoryStream("<svg/>"u8.ToArray()), 0, 6, "imagen", "rostro.png");
        Assert.IsType<BadRequestObjectResult>(await new RostrosController(db).Crear(solicitud, default));
        Assert.Empty(await db.TiposRostro.ToListAsync());
    }

    [Fact]
    public async Task CrearMarco_AceptaRostrosNuevosActivosYRechazaInactivos()
    {
        await using var db = Contexto(); db.TiposRostro.Add(new() { Codigo = "nuevo", Nombre = "Nuevo", Activo = false }); await db.SaveChangesAsync();
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 240 120'><path d='M0 0L20 20'/></svg>";
        var controller = new MarcosController(db);
        Assert.IsType<BadRequestObjectResult>(await controller.Crear(new("Marco", "Marco", svg, ["nuevo"]), default));
        (await db.TiposRostro.SingleAsync()).Activo = true; await db.SaveChangesAsync();
        Assert.IsType<CreatedResult>(await controller.Crear(new("Marco", "Marco", svg, ["nuevo"]), default));
    }

    [Fact]
    public async Task Crear_RechazaMarcosInexistentesSinAsociacionesParciales()
    {
        await using var db = Contexto();
        Assert.IsType<BadRequestObjectResult>(await new RostrosController(db).Crear(Solicitud(999), default));
        Assert.Empty(await db.TiposRostro.ToListAsync());
    }
    [Fact]
    public async Task MarcosPublicos_OcultanAsociacionesConRostrosInactivos()
    {
        await using var db = Contexto();
        db.TiposRostro.AddRange(new TipoRostro { Codigo = "nuevo", Nombre = "Nuevo", Activo = false },
            new TipoRostro { Codigo = "activo", Nombre = "Activo" });
        db.TiposMarco.Add(Marco("redondo", "nuevo,activo"));
        db.Productos.Add(new() { Categoria = "Armazones", IdTipoMarco = db.TiposMarco.Local.Single().Id, Stock = 2, Estado = "Disponible" });
        await db.SaveChangesAsync();
        var controller = new MarcosController(db);
        var publicos = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Disponibles(default)).Result).Value);
        Assert.Equal(new[] { "activo" }, Assert.Single(publicos).Rostros);
        var privados = Assert.IsType<List<TipoMarcoResponse>>(Assert.IsType<OkObjectResult>((await controller.Listar(default)).Result).Value);
        Assert.Equal(new[] { "nuevo", "activo" }, Assert.Single(privados).Rostros);
    }

}
