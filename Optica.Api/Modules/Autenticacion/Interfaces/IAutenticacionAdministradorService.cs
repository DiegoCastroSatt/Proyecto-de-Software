using Optica.Api.Modules.Autenticacion.DTOs;

namespace Optica.Api.Modules.Autenticacion.Interfaces;

public interface IAutenticacionAdministradorService
{
    Task<LoginAdministradorResponse?> IniciarSesionAsync(
        LoginAdministradorRequest request,
        CancellationToken cancellationToken);

    Task<bool> SolicitarRecuperacionAsync(
        string nombreUsuario,
        string? ipCliente,
        CancellationToken cancellationToken);

    Task<bool> ConfirmarRecuperacionAsync(
        string nombreUsuario,
        string codigo,
        string nuevaContrasena,
        string confirmarContrasena,
        CancellationToken cancellationToken);
}
