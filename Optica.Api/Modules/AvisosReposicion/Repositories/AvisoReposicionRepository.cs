using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AvisosReposicion.Interfaces;
using Optica.Api.Modules.AvisosReposicion.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.AvisosReposicion.Repositories;

public sealed class AvisoReposicionRepository(OpticaDbContext db) : IAvisoReposicionRepository
{
    public Task<Cliente?> ObtenerCliente(int idCliente, CancellationToken cancellationToken) =>
        db.Clientes.AsNoTracking().SingleOrDefaultAsync(
            cliente => cliente.IdCliente == idCliente && cliente.Estado == "Activo",
            cancellationToken);

    public Task<Producto?> ObtenerProducto(int idProducto, CancellationToken cancellationToken) =>
        db.Productos.AsNoTracking().SingleOrDefaultAsync(
            producto => producto.IdProducto == idProducto,
            cancellationToken);

    public Task<bool> ExisteAvisoAbierto(string rut, int idProducto, CancellationToken cancellationToken) =>
        db.AvisosReposicion.AnyAsync(
            aviso => aviso.Rut == rut && aviso.IdProducto == idProducto && aviso.Estado == "Abierta",
            cancellationToken);

    public async Task<AvisoReposicion> Crear(AvisoReposicion aviso, CancellationToken cancellationToken)
    {
        db.AvisosReposicion.Add(aviso);
        await db.SaveChangesAsync(cancellationToken);
        return aviso;
    }

    public async Task<IReadOnlyList<AvisoReposicion>> ListarPorCliente(
        int idCliente,
        CancellationToken cancellationToken) =>
        await db.AvisosReposicion.AsNoTracking()
            .Where(aviso => db.Clientes.Any(cliente =>
                cliente.IdCliente == idCliente && cliente.Rut == aviso.Rut))
            .Include(aviso => aviso.Producto)
            .OrderByDescending(aviso => aviso.Fecha)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AvisoReposicion>> ListarTodas(CancellationToken cancellationToken) =>
        await db.AvisosReposicion.AsNoTracking()
            .Include(aviso => aviso.Cliente)
            .Include(aviso => aviso.Producto)
            .OrderByDescending(aviso => aviso.Fecha)
            .ThenByDescending(aviso => aviso.IdAviso)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AvisoReposicion>> ListarPendientesConStock(CancellationToken cancellationToken) =>
        await db.AvisosReposicion
            .Where(aviso => aviso.Estado == "Abierta" && aviso.Producto.Stock > 0)
            .OrderBy(aviso => aviso.Fecha)
            .ThenBy(aviso => aviso.IdAviso)
            .Take(50)
            .Include(aviso => aviso.Cliente)
            .Include(aviso => aviso.Producto)
            .ToListAsync(cancellationToken);

    public async Task Cerrar(AvisoReposicion aviso, CancellationToken cancellationToken)
    {
        aviso.Estado = "Cerrada";
        await db.SaveChangesAsync(cancellationToken);
    }
}
