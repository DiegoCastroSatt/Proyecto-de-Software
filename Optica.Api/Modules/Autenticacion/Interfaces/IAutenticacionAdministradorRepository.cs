using Optica.Api.Modules.AgendaReservas.Models;

namespace Optica.Api.Modules.Autenticacion.Interfaces;

public interface IAutenticacionAdministradorRepository
{
    Task<Administrador?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken);

    Task ActualizarAsync(
        Administrador administrador,
        CancellationToken cancellationToken);
}