using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Controllers;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Xunit;

namespace Optica.Ventas.Tests;

// Base aislada en memoria: valida el controlador, no las restricciones específicas de MySQL.
public class TestClientes : IDisposable
{
    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    public void Dispose() => db.Dispose();
    private ClientesController Controlador() => new(db);
    private static Cliente Valido() => new() { Rut = " 12.345.678-5 ", Nombre = "Ana", Apellido = "Pérez", Correo = " ana@example.test " };

    [Fact]
    public async Task Registrar_NormalizaRutYContactoYAsignaEstado()
    {
        var cliente = Valido(); cliente.Estado = "Inactivo";
        var resultado = Assert.IsType<CreatedAtActionResult>(await Controlador().Registrar(cliente));
        var guardado = await db.Clientes.SingleAsync();
        Assert.Equal("123456785", guardado.Rut);
        Assert.Equal("ana@example.test", guardado.Correo);
        Assert.Equal("Activo", guardado.Estado);
        Assert.NotEqual(default, guardado.FechaRegistro);
        Assert.Null(guardado.Telefono);
        Assert.Same(guardado, resultado.Value);
    }

    [Fact]
    public async Task Registrar_AceptaSoloTelefono()
    {
        var cliente = Valido(); cliente.Correo = null; cliente.Telefono = " 912345678 ";
        Assert.IsType<CreatedAtActionResult>(await Controlador().Registrar(cliente));
        Assert.Equal("912345678", (await db.Clientes.SingleAsync()).Telefono);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public async Task Registrar_RechazaFaltaDeContacto(string? telefono, string? correo)
    {
        var cliente = Valido(); cliente.Telefono = telefono; cliente.Correo = correo;
        Assert.IsType<BadRequestObjectResult>(await Controlador().Registrar(cliente));
        Assert.Empty(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Registrar_RechazaRutInvalido()
    {
        var cliente = Valido(); cliente.Rut = "12345678-9";
        Assert.IsType<BadRequestObjectResult>(await Controlador().Registrar(cliente));
        Assert.Empty(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Registrar_RechazaRutDuplicadoConOtroFormato()
    {
        db.Clientes.Add(new Cliente { Rut = "12.345.678-5", Correo = "otro@example.test" }); await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await Controlador().Registrar(Valido()));
        Assert.Single(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Registrar_RechazaCorreoDuplicado()
    {
        db.Clientes.Add(new Cliente { Rut = "111111111", Correo = "ana@example.test" }); await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await Controlador().Registrar(Valido()));
        Assert.Single(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Actualizar_ConservaRutEstadoYFecha()
    {
        var cliente = Valido(); cliente.FechaRegistro = new DateTime(2025, 1, 1);
        db.Clientes.Add(cliente); await db.SaveChangesAsync();
        var cambios = new Cliente { Rut = "OTRO", Nombre = " María ", Apellido = " Soto ", Correo = " nueva@example.test ", Estado = "Inactivo" };
        Assert.IsType<OkObjectResult>(await Controlador().Actualizar(cliente.IdCliente, cambios));
        db.ChangeTracker.Clear(); var guardado = await db.Clientes.SingleAsync();
        Assert.Equal("María", guardado.Nombre); Assert.Equal("Soto", guardado.Apellido);
        Assert.Equal(cliente.Rut, guardado.Rut); Assert.Equal("Activo", guardado.Estado);
        Assert.Equal(new DateTime(2025, 1, 1), guardado.FechaRegistro);
    }

    [Fact]
    public async Task Actualizar_InexistenteDevuelve404() =>
        Assert.IsType<NotFoundObjectResult>(await Controlador().Actualizar(999, Valido()));

    [Theory]
    [InlineData("Activo")]
    [InlineData("Inactivo")]
    public async Task CambiarEstado_PersisteSinEliminarCliente(string estado)
    {
        var cliente = Valido(); db.Clientes.Add(cliente); await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>(await Controlador().CambiarEstado(cliente.IdCliente, new CambiarEstadoDto { NuevoEstado = estado }));
        db.ChangeTracker.Clear(); Assert.Equal(estado, (await db.Clientes.SingleAsync()).Estado);
    }

    [Fact]
    public async Task CambiarEstado_RechazaEstadoInvalido()
    {
        var cliente = Valido(); db.Clientes.Add(cliente); await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await Controlador().CambiarEstado(cliente.IdCliente, new CambiarEstadoDto { NuevoEstado = "Borrado" }));
        Assert.Equal("Activo", (await db.Clientes.SingleAsync()).Estado);
    }

    [Theory]
    [InlineData("12.345.678-5", true)]
    [InlineData("12345678-9", false)]
    [InlineData(null, false)]
    [InlineData("abc", false)]
    public void Rut_ValidaDigitoVerificador(string? rut, bool valido) => Assert.Equal(valido, RutChilenoValidator.EsValido(rut));

    [Theory]
    [InlineData("+56 9 1234 5678", true)]
    [InlineData("912345678", true)]
    [InlineData("123", false)]
    [InlineData(null, false)]
    public void Telefono_ValidaFormato(string? telefono, bool valido) => Assert.Equal(valido, TelefonoChilenoValidator.EsValido(telefono));
}
