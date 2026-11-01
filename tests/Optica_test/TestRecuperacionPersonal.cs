using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Repositories;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Tests;

public sealed class TestRecuperacionPersonal
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RecuperaPersonalPorUsuarioRevocaSesionYRechazaReuso(bool esAdmin)
    {
        await using var db = new OpticaDbContext(new DbContextOptionsBuilder<OpticaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var hasher = new PasswordHasher<Trabajador>();
        Trabajador cuenta = esAdmin ? new Administrador() : new Trabajador
        { Rol = new RolTrabajador { Nombre = "Ventas", PermisosJson = "[\"ventas\"]" } };
        cuenta.Nombre = "Persona de prueba";
        cuenta.NombreUsuario = "usuario.prueba";
        cuenta.Correo = "compartido@example.test";
        cuenta.Contrasena = hasher.HashPassword(cuenta, "AnteriorClave123!");
        db.Trabajadores.Add(cuenta);
        await db.SaveChangesAsync();
        var email = new CorreoCapturado();
        var estado = new EstadoRecuperacionAdministrador();
        var servicio = new AutenticacionAdministradorService(new AutenticacionAdministradorRepository(db),
            hasher, estado, email, TimeProvider.System, NullLogger<AutenticacionAdministradorService>.Instance);
        var acceso = new AccesoService(db, hasher);
        var sesion = esAdmin
            ? await servicio.IniciarSesionAsync(new() { NombreUsuario = cuenta.NombreUsuario, Contrasena = "AnteriorClave123!" }, default)
            : await acceso.IniciarTrabajadorAsync(new() { NombreUsuario = cuenta.NombreUsuario, Contrasena = "AnteriorClave123!" }, default);
        Assert.NotNull(sesion);
        var principal = AccesoService.CrearPrincipal(sesion);
        Assert.NotNull(await acceso.ObtenerSesionAsync(principal, default));
        Assert.True(await servicio.SolicitarRecuperacionAsync(" USUARIO.PRUEBA ", "127.0.0.1", default));
        Assert.Equal(cuenta.Correo, email.Destinatario);
        var codigo = Regex.Match(email.Html, @">(\d{6})<").Groups[1].Value;
        Assert.Matches(@"^\d{6}$", codigo);
        Assert.True(await servicio.ConfirmarRecuperacionAsync(cuenta.NombreUsuario, codigo, "Abcdef1!", "Abcdef1!", default));
        Assert.False(await servicio.ConfirmarRecuperacionAsync(cuenta.NombreUsuario, codigo, "OtraClave123!", "OtraClave123!", default));
        Assert.Null(await acceso.ObtenerSesionAsync(principal, default));
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(cuenta, cuenta.Contrasena, "Abcdef1!"));
        var nuevoLogin = esAdmin
            ? await servicio.IniciarSesionAsync(new() { NombreUsuario = cuenta.NombreUsuario, Contrasena = "Abcdef1!" }, default)
            : await acceso.IniciarTrabajadorAsync(new() { NombreUsuario = cuenta.NombreUsuario, Contrasena = "Abcdef1!" }, default);
        Assert.NotNull(nuevoLogin);
        Assert.Equal(esAdmin ? "Administrador" : "Trabajador", nuevoLogin.TipoUsuario);
    }

    [Fact]
    public async Task NoEnviaCodigoParaPersonalInactivoNiCorreoComoUsuario()
    {
        await using var db = new OpticaDbContext(new DbContextOptionsBuilder<OpticaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Trabajadores.Add(new Trabajador { NombreUsuario = "inactivo", Correo = "persona@example.test", Estado = "Inactivo" });
        await db.SaveChangesAsync();
        var email = new CorreoCapturado();
        var servicio = new AutenticacionAdministradorService(new AutenticacionAdministradorRepository(db),
            new PasswordHasher<Trabajador>(), new EstadoRecuperacionAdministrador(), email,
            TimeProvider.System, NullLogger<AutenticacionAdministradorService>.Instance);
        Assert.True(await servicio.SolicitarRecuperacionAsync("inactivo", "127.0.0.1", default));
        Assert.True(await servicio.SolicitarRecuperacionAsync("persona@example.test", "127.0.0.1", default));
        Assert.Equal("", email.Html);
    }

    private sealed class CorreoCapturado : IEmailService
    {
        public string? Destinatario { get; private set; }
        public string Html { get; private set; } = "";
        public Task<bool> EnviarAsync(string? destinatario, string asunto, string cuerpoHtml, CancellationToken ct,
            bool importante = false, byte[]? adjunto = null, string? nombreAdjunto = null, string? tipoAdjunto = null)
        {
            Destinatario = destinatario;
            Html = cuerpoHtml;
            return Task.FromResult(true);
        }
    }
}
