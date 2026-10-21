using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Repositories;

public sealed class AutenticacionAdministradorRepository(OpticaDbContext context)
    : IAutenticacionAdministradorRepository
{
    public Task<Administrador?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario,
        CancellationToken cancellationToken) =>
        context.Administradores
            .SingleOrDefaultAsync(administrador => administrador.Nombre == nombreUsuario, cancellationToken);

    public Task<Administrador?> ObtenerPorIdAsync(
        int idAdministrador,
        CancellationToken cancellationToken) =>
        context.Administradores
            .SingleOrDefaultAsync(administrador => administrador.IdAdministrador == idAdministrador, cancellationToken);

    public async Task ActualizarAsync(
        Administrador administrador,
        CancellationToken cancellationToken)
    {
        context.Administradores.Update(administrador);
        await context.SaveChangesAsync(cancellationToken);
    }
}
