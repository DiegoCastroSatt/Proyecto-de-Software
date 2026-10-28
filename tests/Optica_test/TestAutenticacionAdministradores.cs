using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Ventas.Tests;

public sealed class TestAutenticacionAdministradores
{
    private readonly RelojManual reloj = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly RepositorioFalso repositorio = new();
    private readonly CorreoFalso correo = new();
    private readonly EstadoRecuperacionAdministrador estado = new();

    private AutenticacionAdministradorService Servicio() => new(
        repositorio,
        new PasswordHasher<Administrador>(),
        estado,
        correo,
        reloj,
        NullLogger<AutenticacionAdministradorService>.Instance);

    [Fact]
    public async Task IniciarSesion_MigraClaveAntiguaAHash()
    {
        repositorio.Administrador = new Administrador
        {
            IdAdministrador = 7,
            Nombre = "admin",
            Correo = "admin@example.test",
            Contrasena = "ClaveAntigua!",
            Estado = "Activo"
        };

        var sesion = await Servicio().IniciarSesionAsync(
            new LoginAdministradorRequest { NombreUsuario = " admin ", Contrasena = "ClaveAntigua!" },
            CancellationToken.None);

        Assert.NotNull(sesion);
        Assert.Equal(7, sesion.IdAdministrador);
        Assert.StartsWith("AQAAAA", repositorio.Administrador.Contrasena);
        Assert.Equal(1, repositorio.Actualizaciones);
    }

    [Fact]
    public async Task IniciarSesion_RechazaClaveIncorrectaYAdministradorInactivo()
    {
        repositorio.Administrador = CrearAdministrador("ClaveCorrecta123!");

        var incorrecta = await Servicio().IniciarSesionAsync(
            new LoginAdministradorRequest { NombreUsuario = "admin", Contrasena = "ClaveIncorrecta123!" },
            CancellationToken.None);
        repositorio.Administrador.Estado = "Inactivo";
        var inactiva = await Servicio().IniciarSesionAsync(
            new LoginAdministradorRequest { NombreUsuario = "admin", Contrasena = "ClaveCorrecta123!" },
            CancellationToken.None);

        Assert.Null(incorrecta);
        Assert.Null(inactiva);
    }

    [Fact]
    public async Task Recuperacion_CambiaClaveConCodigoEnviadoYDeUnSoloUso()
    {
        repositorio.Administrador = CrearAdministrador("ClaveAnterior123!");
        Assert.True(await Servicio().SolicitarRecuperacionAsync("admin", "127.0.0.1", CancellationToken.None));
        Assert.Equal("admin@example.test", correo.Destinatario);
        var codigo = Regex.Match(correo.CuerpoHtml!, @">(\d{6})<").Groups[1].Value;
        Assert.Matches("^\\d{6}$", codigo);

        var cambiada = await Servicio().ConfirmarRecuperacionAsync(
            "ADMIN", codigo, "NuevaClave123!", "NuevaClave123!", CancellationToken.None);
        var reutilizada = await Servicio().ConfirmarRecuperacionAsync(
            "admin", codigo, "OtraClave123!", "OtraClave123!", CancellationToken.None);

        Assert.True(cambiada);
        Assert.False(reutilizada);
        Assert.StartsWith("AQAAAA", repositorio.Administrador.Contrasena);
        var login = await Servicio().IniciarSesionAsync(
            new LoginAdministradorRequest { NombreUsuario = "admin", Contrasena = "NuevaClave123!" }, CancellationToken.None);
        Assert.NotNull(login);
    }

    [Fact]
    public async Task Recuperacion_ExpiraCodigoYLimitaSolicitudes()
    {
        repositorio.Administrador = CrearAdministrador("ClaveAnterior123!");
        Assert.True(await Servicio().SolicitarRecuperacionAsync("admin", "127.0.0.1", CancellationToken.None));
        Assert.False(await Servicio().SolicitarRecuperacionAsync("admin", "127.0.0.1", CancellationToken.None));
        var codigo = Regex.Match(correo.CuerpoHtml!, @">(\d{6})<").Groups[1].Value;
        reloj.Avanzar(TimeSpan.FromMinutes(6));

        var confirmado = await Servicio().ConfirmarRecuperacionAsync(
            "admin", codigo, "NuevaClave123!", "NuevaClave123!", CancellationToken.None);

        Assert.False(confirmado);
        Assert.Equal(1, correo.Envios);
    }

    [Fact]
    public async Task Recuperacion_NoRevelaSiElAdministradorNoExiste()
    {
        var respuesta = await Servicio().SolicitarRecuperacionAsync("desconocido", "127.0.0.1", CancellationToken.None);

        Assert.True(respuesta);
        Assert.Equal(0, correo.Envios);
    }

    [Fact]
    public void EstadoRecuperacion_InvalidaCodigoTrasTresIntentosIncorrectos()
    {
        var instante = reloj.GetUtcNow();
        Assert.True(estado.IntentarIniciarSolicitud("admin", "127.0.0.1", instante));
        var hashCorrecto = estado.CrearHashCodigo("123456");
        estado.GuardarCodigo("admin", 7, hashCorrecto, instante);

        Assert.False(estado.ValidarCodigo("admin", estado.CrearHashCodigo("000000"), instante).Valido);
        Assert.False(estado.ValidarCodigo("admin", estado.CrearHashCodigo("000001"), instante).Valido);
        Assert.False(estado.ValidarCodigo("admin", estado.CrearHashCodigo("000002"), instante).Valido);
        Assert.False(estado.ValidarCodigo("admin", hashCorrecto, instante).Valido);
    }

    private static Administrador CrearAdministrador(string contrasena)
    {
        var admin = new Administrador
        {
            IdAdministrador = 7,
            Nombre = "admin",
            Correo = "admin@example.test",
            Estado = "Activo"
        };
        admin.Contrasena = new PasswordHasher<Administrador>().HashPassword(admin, contrasena);
        return admin;
    }

    private sealed class RelojManual(DateTimeOffset actual) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => actual;
        public void Avanzar(TimeSpan intervalo) => actual += intervalo;
    }

    private sealed class RepositorioFalso : IAutenticacionAdministradorRepository
    {
        public Administrador? Administrador { get; set; }
        public int Actualizaciones { get; private set; }

        public Task<Administrador?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken) =>
            Task.FromResult(Administrador is not null && string.Equals(Administrador.Nombre, nombreUsuario, StringComparison.OrdinalIgnoreCase)
                ? Administrador : null);

        public Task<Administrador?> ObtenerPorIdAsync(int idAdministrador, CancellationToken cancellationToken) =>
            Task.FromResult(Administrador?.IdAdministrador == idAdministrador ? Administrador : null);

        public Task ActualizarAsync(Administrador administrador, CancellationToken cancellationToken)
        {
            Actualizaciones++;
            Administrador = administrador;
            return Task.CompletedTask;
        }
    }

    private sealed class CorreoFalso : IEmailService
    {
        public string? Destinatario { get; private set; }
        public string? CuerpoHtml { get; private set; }
        public int Envios { get; private set; }

        public Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
            string? destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false,
            byte[]? adjunto = null,
            string? nombreAdjunto = null,
            string? tipoAdjunto = null)
        {
            Destinatario = destinatario;
            CuerpoHtml = cuerpoHtml;
            Envios++;
            return Task.FromResult(new ResultadoEnvioCorreo(true));
        }

        public Task<bool> EnviarAsync(
            string? destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false,
            byte[]? adjunto = null,
            string? nombreAdjunto = null,
            string? tipoAdjunto = null) => Task.FromResult(true);
    }
}
