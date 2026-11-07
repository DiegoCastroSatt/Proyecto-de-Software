using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;
using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Repositories;
using Optica.Api.Modules.Resenas.Services;
using Optica.Api.Modules.Ventas.Models;
using Xunit;

namespace Optica.Tests;

public sealed class TestResenas
{
    private static (OpticaDbContext Db, ResenaService Service) CrearServicio()
    {
        var opciones = new DbContextOptionsBuilder<OpticaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new OpticaDbContext(opciones);
        db.Clientes.Add(new Cliente { IdCliente = 1, Rut = "11111111-1", Nombre = "Ana", Apellido = "Pérez", Estado = "Activo" });
        db.Clientes.Add(new Cliente { IdCliente = 2, Rut = "22222222-2", Nombre = "Luis", Apellido = "Díaz", Estado = "Activo" });
        db.Productos.Add(new Producto { IdProducto = 10, Codigo = "OPT-10", Nombre = "Armazón", Categoria = "Armazones" });
        db.Ventas.Add(new Venta
        {
            IdVenta = 20,
            ClienteId = 1,
            Fecha = DateTime.UtcNow,
            Detalles = [new DetalleVenta { IdDetalle = 30, ProductoId = 10, NombreProducto = "Armazón", Cantidad = 1 }]
        });
        db.SaveChanges();
        return (db, new ResenaService(new ResenaRepository(db), TimeProvider.System));
    }

    [Fact]
    public async Task ClienteConVentaRegistradaPuedePublicarYQuedaAlmacenada()
    {
        var (db, servicio) = CrearServicio();
        var creada = await servicio.Crear(1, 10, new CrearResenaDto { Calificacion = 5, Comentario = "Muy cómodos" }, CancellationToken.None);

        Assert.Equal(5, creada.Calificacion);
        Assert.Equal(1, await db.Resenas.CountAsync());
        Assert.Equal(1, (await servicio.ObtenerResumen(10, CancellationToken.None)).Cantidad);
    }

    [Fact]
    public async Task ClienteSinCompraNoPuedePublicar()
    {
        var (_, servicio) = CrearServicio();
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.Crear(2, 10,
            new CrearResenaDto { Calificacion = 4, Comentario = "Buen producto" }, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, "Buen producto")]
    [InlineData(6, "Buen producto")]
    [InlineData(3, "   ")]
    [InlineData(3, "ok")]
    public async Task RechazaCalificacionYComentarioInvalidos(int calificacion, string comentario)
    {
        var (_, servicio) = CrearServicio();
        await Assert.ThrowsAsync<ArgumentException>(() => servicio.Crear(1, 10,
            new CrearResenaDto { Calificacion = calificacion, Comentario = comentario }, CancellationToken.None));
    }

    [Fact]
    public async Task RechazaUnaSegundaResenaDelMismoCliente()
    {
        var (_, servicio) = CrearServicio();
        var dto = new CrearResenaDto { Calificacion = 4, Comentario = "Buen producto" };
        await servicio.Crear(1, 10, dto, CancellationToken.None);

        await Assert.ThrowsAsync<ResenaDuplicadaException>(() => servicio.Crear(1, 10, dto, CancellationToken.None));
    }
}
