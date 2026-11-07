using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Tests;

public sealed class TestRecuperacionEnMemoria : IDisposable
{
    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);
    private readonly EstadoRecuperacion estado = new();
    private readonly RelojManual reloj = new(DateTimeOffset.UtcNow);
    private readonly CorreoCapturado correo = new();
    private readonly PasswordHasher<CuentaCliente> hasher = new();
    private readonly CuentaCliente cuenta;

    public TestRecuperacionEnMemoria()
    {
        var cliente = new Cliente { Nombre = "Ana", Apellido = "Pérez", Rut = "12345678-5", Estado = "Activo" };
        cuenta = new CuentaCliente
        {
            Cliente = cliente,
            Correo = "ana@example.test",
            CorreoNormalizado = "ANA@EXAMPLE.TEST",
            Estado = "Activa"
        };
        cuenta.ContrasenaHash = hasher.HashPassword(cuenta, "ClaveAnterior123!");
        db.CuentasCliente.Add(cuenta);
        db.SaveChanges();
    }

    [Fact]
    public async Task SolicitarCodigoPorRut_PermiteCambiarClaveUnaSolaVez()
    {
        await Servicio().SolicitarAsync("12.345.678-5", CancellationToken.None);
        var codigo = Regex.Match(correo.Cuerpo, @">(\d{6})<").Groups[1].Value;
        Assert.Matches("^[0-9]{6}$", codigo);

        var cambiado = await Servicio().ConfirmarAsync(
            "12345678-5", codigo, "ClaveNueva123!", "ClaveNueva123!", CancellationToken.None);
        var reutilizado = await Servicio().ConfirmarAsync(
            "12345678-5", codigo, "OtraClave123!", "OtraClave123!", CancellationToken.None);

        Assert.True(cambiado);
        Assert.False(reutilizado);
        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(cuenta, cuenta.ContrasenaHash, "ClaveNueva123!"));
    }

    [Fact]
    public async Task SolicitarOtroCodigo_InvalidaElAnterior()
    {
        await Servicio().SolicitarAsync("12345678-5", CancellationToken.None);
        var anterior = Regex.Match(correo.Cuerpo, @">(\d{6})<").Groups[1].Value;
        await Servicio().SolicitarAsync("12345678-5", CancellationToken.None);
        var nuevo = Regex.Match(correo.Cuerpo, @">(\d{6})<").Groups[1].Value;
        Assert.Matches("^[0-9]{6}$", anterior);
        Assert.Matches("^[0-9]{6}$", nuevo);
        Assert.NotEqual(anterior, nuevo);

        Assert.False(await Servicio().ConfirmarAsync(
            "12345678-5", anterior, "ClaveNueva123!", "ClaveNueva123!", CancellationToken.None));
        Assert.True(await Servicio().ConfirmarAsync(
            "12345678-5", nuevo, "ClaveNueva123!", "ClaveNueva123!", CancellationToken.None));
    }

    private RecuperacionService Servicio() => new(
        db,
        estado,
        hasher,
        new PasswordHasher<Trabajador>(),
        correo,
        reloj,
        NullLogger<RecuperacionService>.Instance);

    public void Dispose() => db.Dispose();

    private sealed class RelojManual(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }

    private sealed class CorreoCapturado : IEmailService
    {
        public string Cuerpo { get; private set; } = "";

        public Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
            string destinatario,
            string asunto,
            string cuerpoHtml,
            CancellationToken cancellationToken,
            bool importante = false)
        {
            Cuerpo = cuerpoHtml;
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
            Cuerpo = cuerpoHtml;
            return Task.FromResult(true);
        }
    }
}
