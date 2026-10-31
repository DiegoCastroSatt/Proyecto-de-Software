using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Repositories;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Tests;

public sealed class TestInicioSesion : IDisposable
{
    private const string Clave = "PruebaAcceso123!";
    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private InicioSesionService Servicio()
    {
        var correo = new CorreoPrueba();
        var admin = new AutenticacionAdministradorService(new AutenticacionAdministradorRepository(db),
            new PasswordHasher<Administrador>(), new EstadoRecuperacionAdministrador(), correo,
            TimeProvider.System, NullLogger<AutenticacionAdministradorService>.Instance);
        var clientes = new AutenticacionClienteService(db, new PasswordHasher<CuentaCliente>(),
            new EstadoRecuperacionCliente(), correo, TimeProvider.System, NullLogger<AutenticacionClienteService>.Instance, new ConfigurationBuilder().Build());
        return new(db, admin, new AccesoService(db, new PasswordHasher<Trabajador>()), clientes);
    }

    private async Task Preparar()
    {
        var admin = new Administrador { Nombre = "admin", Correo = "admin@example.test" };
        admin.Contrasena = new PasswordHasher<Administrador>().HashPassword(admin, Clave);
        db.Administradores.Add(admin);
        var trabajador = new Trabajador { Nombre = "Equipo", Correo = "EQUIPO@EXAMPLE.TEST",
            Rol = new RolTrabajador { Nombre = "Ventas", PermisosJson = "[\"ventas\"]" } };
        trabajador.Contrasena = new PasswordHasher<Trabajador>().HashPassword(trabajador, Clave);
        db.Trabajadores.Add(trabajador);
        var cuenta = new CuentaCliente { Correo = "cliente@example.test", CorreoNormalizado = "CLIENTE@EXAMPLE.TEST",
            Cliente = new Cliente { Nombre = "Ana", Apellido = "Pérez", Rut = "12345678-5", Estado = "Activo" } };
        cuenta.ContrasenaHash = new PasswordHasher<CuentaCliente>().HashPassword(cuenta, Clave);
        db.CuentasCliente.Add(cuenta);
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(" ADMIN@example.test ", "Administrador")]
    [InlineData("admin", "Administrador")]
    [InlineData("equipo@example.test", "Trabajador")]
    [InlineData("Cliente@example.test", "Cliente")]
    public async Task ReconoceElTipoSoloDespuesDeValidarLaClave(string correo, string tipo)
    {
        await Preparar();
        var resultado = await Servicio().IniciarAsync(new LoginClienteRequest { Correo = correo, Contrasena = Clave }, default);
        Assert.Equal(tipo, resultado?.TipoUsuario);
        if (tipo == "Cliente") { Assert.Null(resultado!.Personal); Assert.NotNull(resultado.Cliente); }
        else { Assert.NotNull(resultado!.Personal); Assert.Null(resultado.Cliente); }
        if (tipo == "Trabajador") Assert.Equal(new[] { "ventas" }, resultado!.Personal!.Permisos);
    }

    [Theory]
    [InlineData("admin@example.test")]
    [InlineData("equipo@example.test")]
    [InlineData("cliente@example.test")]
    [InlineData("inexistente@example.test")]
    public async Task RechazaClaveIncorrectaSinRevelarElTipo(string correo)
    {
        await Preparar();
        Assert.Null(await Servicio().IniciarAsync(new LoginClienteRequest { Correo = correo, Contrasena = "incorrecta" }, default));
    }

    [Fact]
    public async Task RechazaTrabajadorInactivo()
    {
        await Preparar();
        (await db.Trabajadores.SingleAsync()).Estado = "Inactivo";
        await db.SaveChangesAsync();
        Assert.Null(await Servicio().IniciarAsync(new LoginClienteRequest { Correo = "equipo@example.test", Contrasena = Clave }, default));
    }

    [Fact]
    public async Task NoEligeUnaCuentaPorPrioridadSiLasCredencialesSonAmbiguas()
    {
        await Preparar();
        var cuenta = await db.CuentasCliente.SingleAsync();
        cuenta.Correo = "equipo@example.test"; cuenta.CorreoNormalizado = "EQUIPO@EXAMPLE.TEST";
        await db.SaveChangesAsync();
        Assert.Null(await Servicio().IniciarAsync(new LoginClienteRequest { Correo = cuenta.Correo, Contrasena = Clave }, default));
        cuenta.ContrasenaHash = new PasswordHasher<CuentaCliente>().HashPassword(cuenta, "OtraClave123!");
        await db.SaveChangesAsync();
        var resultado = await Servicio().IniciarAsync(new LoginClienteRequest { Correo = cuenta.Correo, Contrasena = Clave }, default);
        Assert.Equal("Trabajador", resultado?.TipoUsuario);
    }

    public void Dispose() => db.Dispose();
    private sealed class CorreoPrueba : IEmailService
    {
        public Task<bool> EnviarAsync(string? destinatario, string asunto, string cuerpoHtml, CancellationToken ct,
            bool importante = false, byte[]? adjunto = null, string? nombreAdjunto = null, string? tipoAdjunto = null)
            => Task.FromResult(true);
    }
}
