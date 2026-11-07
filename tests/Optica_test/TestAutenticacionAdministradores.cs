using Optica.Api.Modules.Accesos;
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
    private readonly RepositorioFalso repositorio = new();

    private AutenticacionAdministradorService Servicio() => new(
        repositorio,
        new PasswordHasher<Trabajador>());

    [Fact]
    public async Task IniciarSesion_MigraClaveAntiguaAHash()
    {
        repositorio.Administrador = new Administrador
        {
            IdAdministrador = 7,
            Nombre = "admin", NombreUsuario = "admin",
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

    [Theory]
    [InlineData("abcdef1!", true)]
    [InlineData("ABCDEF1!", true)]
    [InlineData("abcde1!", false)]
    [InlineData("abcdefgh!", false)]
    [InlineData("1234567!", false)]
    [InlineData("abcdef12", false)]
    [InlineData("abcdef1 ", false)]
    public void ContrasenaDelPersonalUsaLosMismosRequisitosDelCliente(string clave, bool valida)
    {
        Assert.Equal(valida, AutenticacionClienteService.ContrasenaValida(clave));
    }

    private static Administrador CrearAdministrador(string contrasena)
    {
        var admin = new Administrador
        {
            IdAdministrador = 7,
            Nombre = "admin", NombreUsuario = "admin",
            Correo = "admin@example.test",
            Estado = "Activo"
        };
        admin.Contrasena = new PasswordHasher<Trabajador>().HashPassword(admin, contrasena);
        return admin;
    }

    private sealed class RepositorioFalso : IAutenticacionAdministradorRepository
    {
        public Administrador? Administrador { get; set; }
        public int Actualizaciones { get; private set; }

        public Task<Trabajador?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken) =>
            Task.FromResult<Trabajador?>(Administrador is not null && string.Equals(Administrador.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)
                ? Administrador : null);

        public Task<Trabajador?> ObtenerPorIdAsync(int idAdministrador, CancellationToken cancellationToken) =>
            Task.FromResult<Trabajador?>(Administrador?.IdAdministrador == idAdministrador ? Administrador : null);

        public Task ActualizarAsync(Trabajador administrador, CancellationToken cancellationToken)
        {
            Actualizaciones++;
            Administrador = (Administrador)administrador;
            return Task.CompletedTask;
        }
    }

}
