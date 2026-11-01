using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Services;

public sealed record ResultadoInicioSesion(string TipoUsuario, LoginAdministradorResponse? Personal = null,
    ClienteSesionResponse? Cliente = null);

public sealed class InicioSesionService(IAutenticacionAdministradorService administradores, AccesoService trabajadores,
    AutenticacionClienteService clientes)
{
    public async Task<ResultadoInicioSesion?> IniciarAsync(LoginClienteRequest request, CancellationToken ct)
    {
        var identificador = request.Correo.Trim();
        // El correo identifica clientes; el nombre de usuario identifica exclusivamente personal.
        if (identificador.Contains('@'))
        {
            var (cliente, valida) = await clientes.IniciarSesionAsync(request, ct);
            return valida && cliente is not null ? new("Cliente", Cliente: cliente) : null;
        }
        var credenciales = new LoginAdministradorRequest { NombreUsuario = identificador, Contrasena = request.Contrasena };
        var admin = await administradores.IniciarSesionAsync(credenciales, ct);
        if (admin is not null) return new("Administrador", Personal: admin);
        var trabajador = await trabajadores.IniciarTrabajadorAsync(credenciales, ct);
        return trabajador is null ? null : new("Trabajador", Personal: trabajador);
    }
}
