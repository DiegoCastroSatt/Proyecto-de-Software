using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Controllers;
using Optica.Api.Modules.Clientes.DTOs;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Repositories;
using Optica.Api.Modules.Clientes.Services;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestClientes : IDisposable
{
    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public void Dispose() => db.Dispose();

    private ClientesController Controlador()
    {
        var repo = new ClienteRepository(db);
        var service = new ClienteService(repo);
        return new ClientesController(service);
    }

    private static CrearClienteDto ValidoDto() => new()
    {
        Rut = " 12.345.678-5 ",
        Nombre = "Ana",
        Apellido = "Pérez",
        Correo = " ana@example.test "
    };

    [Fact]
    public async Task Registrar_NormalizaRutYContactoYAsignaEstado()
    {
        var dto = ValidoDto();
        var resultado = Assert.IsType<CreatedAtActionResult>(await Controlador().Registrar(dto));
        var guardado = await db.Clientes.SingleAsync();

        // Tu servicio elimina espacios y puntos, conservando el guion
        Assert.Equal("12345678-5", guardado.Rut);
        Assert.Equal("ana@example.test", guardado.Correo);
        Assert.Equal("Activo", guardado.Estado);
        Assert.NotEqual(default, guardado.FechaRegistro);
        Assert.Null(guardado.Telefono);
    }

    [Fact]
    public async Task Registrar_AceptaSoloTelefono()
    {
        var dto = ValidoDto();
        dto.Correo = null;
        dto.Telefono = " 912345678 ";

        Assert.IsType<CreatedAtActionResult>(await Controlador().Registrar(dto));
        Assert.Equal("912345678", (await db.Clientes.SingleAsync()).Telefono);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public async Task Registrar_RechazaFaltaDeContacto(string? telefono, string? correo)
    {
        var dto = ValidoDto();
        dto.Telefono = telefono;
        dto.Correo = correo;

        Assert.IsType<BadRequestObjectResult>(await Controlador().Registrar(dto));
        Assert.Empty(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Registrar_RechazaRutDuplicado()
    {
        // Se siembra el cliente con el formato limpio que maneja el repositorio
        db.Clientes.Add(new Cliente { Rut = "12345678-5", Correo = "otro@example.test", Nombre = "Otro", Apellido = "Cliente" });
        await db.SaveChangesAsync();

        // ValidoDto() tiene " 12.345.678-5 ", que limpia a "12345678-5" y debe disparar Conflict
        Assert.IsType<ConflictObjectResult>(await Controlador().Registrar(ValidoDto()));
        Assert.Single(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Actualizar_ConservaRutEstadoYFecha()
    {
        var cliente = new Cliente
        {
            Rut = "12345678-5",
            Nombre = "Ana",
            Apellido = "Pérez",
            Correo = "ana@example.test",
            Estado = "Activo",
            FechaRegistro = new DateTime(2025, 1, 1)
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var cambios = new ActualizarClienteDto
        {
            Nombre = " María ",
            Apellido = " Soto ",
            Correo = " nueva@example.test "
        };

        Assert.IsType<OkObjectResult>(await Controlador().Actualizar(cliente.IdCliente, cambios));

        db.ChangeTracker.Clear();
        var guardado = await db.Clientes.SingleAsync();
        Assert.Equal("María", guardado.Nombre);
        Assert.Equal("Soto", guardado.Apellido);
        Assert.Equal(cliente.Rut, guardado.Rut);
        Assert.Equal("Activo", guardado.Estado);
        Assert.Equal(new DateTime(2025, 1, 1), guardado.FechaRegistro);
    }

    [Fact]
    public async Task Actualizar_InexistenteDevuelve404()
    {
        var cambios = new ActualizarClienteDto
        {
            Nombre = "Prueba",
            Apellido = "Inexistente",
            Correo = "prueba@example.test"
        };
        Assert.IsType<NotFoundObjectResult>(await Controlador().Actualizar(999, cambios));
    }

    [Theory]
    [InlineData("Activo")]
    [InlineData("Inactivo")]
    public async Task CambiarEstado_PersisteSinEliminarCliente(string estado)
    {
        var cliente = new Cliente { Rut = "12345678-5", Nombre = "Ana", Apellido = "Pérez", Correo = "ana@example.test", Estado = "Activo" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        Assert.IsType<OkObjectResult>(await Controlador().CambiarEstado(cliente.IdCliente, new CambiarEstadoDto { NuevoEstado = estado }));
        db.ChangeTracker.Clear();
        Assert.Equal(estado, (await db.Clientes.SingleAsync()).Estado);
    }

    [Fact]
    public async Task CambiarEstado_RechazaEstadoInvalido()
    {
        var cliente = new Cliente { Rut = "12345678-5", Nombre = "Ana", Apellido = "Pérez", Correo = "ana@example.test", Estado = "Activo" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

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