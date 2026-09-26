using Optica.Api.Modules.Autenticacion.DTOs;

namespace Optica.Api.Modules.Autenticacion.Interfaces;

public interface IAutenticacionAdministradorService
{
    Task<LoginAdministradorResponse?> IniciarSesionAsync(
        LoginAdministradorRequest request,
        CancellationToken cancellationToken);
}