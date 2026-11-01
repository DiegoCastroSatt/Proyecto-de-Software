using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Repositories;

public sealed class AutenticacionAdministradorRepository(OpticaDbContext context)
    : IAutenticacionAdministradorRepository
{
    public Task<Trabajador?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken) =>
        context.Trabajadores
            .SingleOrDefaultAsync(administrador => administrador.NombreUsuario.ToUpper() == nombreUsuario.ToUpper(), cancellationToken);

    public Task<Trabajador?> ObtenerPorIdAsync(
        int idAdministrador,
        CancellationToken cancellationToken) =>
        context.Trabajadores
            .SingleOrDefaultAsync(administrador => administrador.IdTrabajador == idAdministrador, cancellationToken);

    public async Task ActualizarAsync(
        Trabajador administrador,
        CancellationToken cancellationToken)
    {
        context.Trabajadores.Update(administrador);
        await context.SaveChangesAsync(cancellationToken);
    }
}
