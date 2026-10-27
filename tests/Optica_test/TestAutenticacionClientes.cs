using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Ventas.Tests;

public sealed class TestAutenticacionClientes : IDisposable
{
    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private readonly EstadoRecuperacionCliente estadoRecuperacion = new();

    private AutenticacionClienteService Servicio(IEmailService? emailService = null) =>
        new(
            db,
            new PasswordHasher<CuentaCliente>(),
            estadoRecuperacion,
            emailService ?? new FakeEmailService(),
            TimeProvider.System,
            NullLogger<AutenticacionClienteService>.Instance);

    private static RegistroClienteRequest Registro(string rut = "12345678-5", string correo = "ana@example.test") => new()
    {
        Nombre = "Ana",
        Apellido = "Pérez",
        Rut = rut,
        Correo = correo,
        Telefono = "912345678",
        Contrasena = "Optica123!",
        ConfirmarContrasena = "Optica123!"
    };

    [Fact]
    public async Task Registrar_CreaClienteYGuardaHashDeContrasena()
    {
        var resultado = await Servicio().RegistrarAsync(Registro(), CancellationToken.None);

        Assert.True(resultado.Registrado);
        var cliente = await db.Clientes.SingleAsync();
        var cuenta = await db.CuentasCliente.SingleAsync();
        Assert.Equal(cliente.IdCliente, cuenta.IdCliente);
        Assert.NotEqual("Optica123!", cuenta.ContrasenaHash);
        Assert.StartsWith("AQAAAA", cuenta.ContrasenaHash);
    }

    [Fact]
    public async Task Registrar_RechazaRutExistenteSinCrearOtraPersona()
    {
        db.Clientes.Add(new Cliente { Rut = "12345678-5", Nombre = "Ana", Apellido = "Existente" });
        await db.SaveChangesAsync();

        var resultado = await Servicio().RegistrarAsync(Registro(), CancellationToken.None);

        Assert.True(resultado.ConflictoExistente);
        Assert.Single(await db.Clientes.ToListAsync());
        Assert.Empty(await db.CuentasCliente.ToListAsync());
    }

    [Fact]
    public async Task Registrar_RechazaCorreoExistenteSinVincularCuenta()
    {
        db.Clientes.Add(new Cliente { Rut = "11111111-1", Nombre = "Otra", Apellido = "Persona", Correo = "ANA@example.test" });
        await db.SaveChangesAsync();

        var resultado = await Servicio().RegistrarAsync(Registro("12345678-5", "ana@example.test"), CancellationToken.None);

        Assert.True(resultado.ConflictoExistente);
        Assert.Single(await db.Clientes.ToListAsync());
        Assert.Empty(await db.CuentasCliente.ToListAsync());
    }

    [Fact]
    public async Task IniciarSesion_RechazaClaveIncorrectaYAceptaLaCorrecta()
    {
        await Servicio().RegistrarAsync(Registro(), CancellationToken.None);
        var servicio = Servicio();

        var incorrecta = await servicio.IniciarSesionAsync(
            new LoginClienteRequest { Correo = "ANA@example.test", Contrasena = "otraClave123!" }, CancellationToken.None);
        var correcta = await servicio.IniciarSesionAsync(
            new LoginClienteRequest { Correo = "ANA@example.test", Contrasena = "Optica123!" }, CancellationToken.None);

        Assert.False(incorrecta.CredencialesValidas);
        Assert.True(correcta.CredencialesValidas);
        Assert.Equal("Ana Pérez", correcta.Sesion?.Nombre);
    }

    [Fact]
    public async Task Registrar_RechazaContrasenaQueNoCumplePolitica()
    {
        var request = Registro() with { Contrasena = "soloLetras", ConfirmarContrasena = "soloLetras" };

        var resultado = await Servicio().RegistrarAsync(request, CancellationToken.None);

        Assert.False(resultado.Registrado);
        Assert.NotNull(resultado.Error);
        Assert.Empty(await db.Clientes.ToListAsync());
    }

    [Fact]
    public async Task Recuperacion_SolicitarYConfirmar_ActualizaContrasena()
    {
        await Servicio().RegistrarAsync(Registro(), CancellationToken.None);

        var fakeEmail = new FakeEmailService();
        var servicio = Servicio(fakeEmail);

        var solicitada = await servicio.SolicitarRecuperacionAsync("ana@example.test", "127.0.0.1", CancellationToken.None);
        Assert.True(solicitada);
        Assert.NotNull(fakeEmail.UltimoCuerpoHtml);

        // Extraer el token del HTML generado
        var marcador = "token=";
        var indiceToken = fakeEmail.UltimoCuerpoHtml.IndexOf(marcador, StringComparison.Ordinal);
        Assert.True(indiceToken >= 0);
        var finToken = fakeEmail.UltimoCuerpoHtml.IndexOfAny(['"', '&', '\'', ' '], indiceToken);
        var token = fakeEmail.UltimoCuerpoHtml[(indiceToken + marcador.Length)..finToken];

        var confirmada = await servicio.ConfirmarRecuperacionAsync(token, "NuevaClave999!", "NuevaClave999!", CancellationToken.None);
        Assert.True(confirmada);

        // Verificar que puede iniciar sesión con la nueva clave
        var login = await servicio.IniciarSesionAsync(
            new LoginClienteRequest { Correo = "ana@example.test", Contrasena = "NuevaClave999!" }, CancellationToken.None);
        Assert.True(login.CredencialesValidas);
    }

    public void Dispose() => db.Dispose();

    private sealed class FakeEmailService : IEmailService
    {
        public string? UltimoCuerpoHtml { get; private set; }

        public Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false)
        {
            UltimoCuerpoHtml = cuerpoHtml;
            return Task.FromResult(new ResultadoEnvioCorreo(true));
        }

        public Task<bool> EnviarAsync(
            string? destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false,
            byte[]? adjuntoBytes = null,
            string? nombreArchivoAdjunto = null,
            string? tipoContenidoAdjunto = null)
        {
            UltimoCuerpoHtml = cuerpoHtml;
            return Task.FromResult(true);
        }
    }
}