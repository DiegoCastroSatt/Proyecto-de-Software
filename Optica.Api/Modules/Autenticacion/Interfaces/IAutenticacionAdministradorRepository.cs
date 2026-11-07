using Optica.Api.Modules.Accesos;

namespace Optica.Api.Modules.Autenticacion.Interfaces;

public interface IAutenticacionAdministradorRepository
{
    Task<Trabajador?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken);

    Task<Trabajador?> ObtenerPorIdAsync(
        int idAdministrador,
        CancellationToken cancellationToken);

    Task ActualizarAsync(
        Trabajador administrador,
        CancellationToken cancellationToken);
}
