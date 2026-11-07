using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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

    private readonly RelojManual reloj = new(DateTimeOffset.UtcNow);

    private AutenticacionClienteService Servicio(IEmailService? emailService = null) =>
        new(
            db,
            new PasswordHasher<CuentaCliente>(),
            emailService ?? new FakeEmailService(),
            reloj,
            NullLogger<AutenticacionClienteService>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Correo:UrlBaseFrontend"] = "http://localhost:4200"
            }).Build());

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
    public async Task Registrar_CreaCuentaPendienteYGuardaHashDeContrasena()
    {
        var fakeEmail = new FakeEmailService();
        var resultado = await Servicio(fakeEmail).RegistrarAsync(Registro(), CancellationToken.None);

        Assert.True(resultado.Registrado);
        Assert.True(resultado.CorreoConfirmacionEnviado);
        var cliente = await db.Clientes.SingleAsync();
        var cuenta = await db.CuentasCliente.SingleAsync();
        Assert.Equal(cliente.IdCliente, cuenta.IdCliente);
        Assert.Equal("PendienteConfirmacion", cuenta.Estado);
        Assert.NotNull(cuenta.TokenConfirmacionHash);
        Assert.NotEqual("Optica123!", cuenta.ContrasenaHash);
        Assert.StartsWith("AQAAAA", cuenta.ContrasenaHash);

        var token = ExtraerToken(fakeEmail.UltimoCuerpoHtml!, "confirmar-cuenta?token=");
        Assert.True(await Servicio(fakeEmail).ConfirmarCuentaAsync(token, CancellationToken.None));
        Assert.Equal("Activa", (await db.CuentasCliente.SingleAsync()).Estado);
        Assert.False(await Servicio(fakeEmail).ConfirmarCuentaAsync(token, CancellationToken.None));
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
        var fakeEmail = new FakeEmailService();
        await Servicio(fakeEmail).RegistrarAsync(Registro(), CancellationToken.None);
        var token = ExtraerToken(fakeEmail.UltimoCuerpoHtml!, "confirmar-cuenta?token=");
        await Servicio().ConfirmarCuentaAsync(token, CancellationToken.None);
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
    public async Task IniciarSesion_RechazaCuentaHastaConfirmarCorreo()
    {
        await Servicio().RegistrarAsync(Registro(), CancellationToken.None);

        var resultado = await Servicio().IniciarSesionAsync(
            new LoginClienteRequest { Correo = "ana@example.test", Contrasena = "Optica123!" }, CancellationToken.None);

        Assert.False(resultado.CredencialesValidas);
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
    public async Task ConfirmarCuenta_RechazaTokenExpiradoSinActivarLaCuenta()
    {
        var fakeEmail = new FakeEmailService();
        await Servicio(fakeEmail).RegistrarAsync(Registro(), CancellationToken.None);
        var cuenta = await db.CuentasCliente.SingleAsync();
        cuenta.TokenConfirmacionExpiraUtc = reloj.GetUtcNow().UtcDateTime.AddSeconds(-1);
        await db.SaveChangesAsync();
        var token = ExtraerToken(fakeEmail.UltimoCuerpoHtml!, "confirmar-cuenta?token=");

        var resultado = await Servicio(fakeEmail).ConfirmarCuentaAsync(token, CancellationToken.None);

        Assert.False(resultado);
        Assert.Equal("PendienteConfirmacion", (await db.CuentasCliente.SingleAsync()).Estado);
    }

    [Fact]
    public async Task Registro_SiFallaCorreoConservaCuentaPendienteYPuedeReenviar()
    {
        var emailFallido = new FakeEmailService { Exito = false };
        var resultadoRegistro = await Servicio(emailFallido).RegistrarAsync(Registro(), CancellationToken.None);
        var cuenta = await db.CuentasCliente.SingleAsync();
        Assert.True(resultadoRegistro.Registrado);
        Assert.False(resultadoRegistro.CorreoConfirmacionEnviado);
        Assert.Equal("PendienteConfirmacion", cuenta.Estado);

        cuenta.UltimoCorreoConfirmacionUtc = reloj.GetUtcNow().UtcDateTime.AddMinutes(-2);
        var tokenAnteriorHash = cuenta.TokenConfirmacionHash;
        await db.SaveChangesAsync();
        var emailReenviado = new FakeEmailService();
        await Servicio(emailReenviado).ReenviarConfirmacionAsync("ANA@example.test", CancellationToken.None);

        Assert.Contains("confirmar-cuenta?token=", emailReenviado.UltimoCuerpoHtml);
        var tokenNuevoHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(ExtraerToken(emailReenviado.UltimoCuerpoHtml!, "confirmar-cuenta?token="))))
            .ToLowerInvariant();
        Assert.NotEqual(tokenAnteriorHash, tokenNuevoHash);
        Assert.Equal(tokenNuevoHash, cuenta.TokenConfirmacionHash);
    }

    private static string ExtraerToken(string html, string marcador)
    {
        var indiceToken = html.IndexOf(marcador, StringComparison.Ordinal);
        Assert.True(indiceToken >= 0);
        var inicio = indiceToken + marcador.Length;
        var finToken = html.IndexOfAny(['"', '&', '\'', ' '], inicio);
        if (finToken < 0) finToken = html.Length;
        return Uri.UnescapeDataString(html[inicio..finToken]);
    }

    private sealed class RelojManual(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    public void Dispose() => db.Dispose();

    private sealed class FakeEmailService : IEmailService
    {
        public string? UltimoCuerpoHtml { get; private set; }
        public bool Exito { get; init; } = true;

        public Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false)
        {
            UltimoCuerpoHtml = cuerpoHtml;
            return Task.FromResult(new ResultadoEnvioCorreo(Exito));
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
            return Task.FromResult(Exito);
        }
    }
}
