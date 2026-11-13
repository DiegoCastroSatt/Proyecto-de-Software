using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Interfaces;
using Optica.Api.Modules.Resenas.Models;

namespace Optica.Api.Modules.Resenas.Repositories;

public sealed class ResenaRepository(OpticaDbContext db) : IResenaRepository
{
    public async Task<IReadOnlyList<ResenaResponseDto>> ListarPorProducto(int idProducto, CancellationToken cancellationToken) =>
        await db.Resenas.AsNoTracking().Where(r => r.IdProducto == idProducto)
            .OrderByDescending(r => r.FechaCreacionUtc)
            .ThenByDescending(r => r.IdResena)
            .Select(r => new ResenaResponseDto(r.IdResena, r.IdProducto, r.Calificacion, r.Comentario,
                // Las reseñas de clientes que eliminaron su cuenta se muestran sin identificar.
                r.Cliente.Rut.StartsWith(EliminacionCuentaClienteService.PrefijoRutAnonimo)
                    ? "Cliente anónimo"
                    : r.Cliente.Nombre + " " + r.Cliente.Apellido.Substring(0, 1) + ".",
                r.FechaCreacionUtc, r.FechaActualizacionUtc))
            .ToListAsync(cancellationToken);

    public async Task<ResumenResenasDto> ObtenerResumen(int idProducto, CancellationToken cancellationToken)
    {
        var resumen = await db.Resenas.AsNoTracking()
            .Where(r => r.IdProducto == idProducto)
            .GroupBy(r => r.IdProducto)
            .Select(grupo => new { Cantidad = grupo.Count(), Promedio = grupo.Average(r => (double)r.Calificacion) })
            .SingleOrDefaultAsync(cancellationToken);
        return resumen is null
            ? new ResumenResenasDto(0, 0)
            : new ResumenResenasDto(Math.Round(resumen.Promedio, 1), resumen.Cantidad);
    }

    public async Task<IReadOnlyList<ProductoResenableDto>> ListarProductosComprados(int idCliente, CancellationToken cancellationToken) =>
        await db.Ventas.AsNoTracking().Where(v => v.ClienteId == idCliente)
            .SelectMany(v => v.Detalles).Where(d => d.ProductoId.HasValue)
            .Select(d => d.ProductoId!.Value).Distinct()
            .Select(idProducto => new ProductoResenableDto(idProducto,
                db.Resenas.Any(r => r.IdCliente == idCliente && r.IdProducto == idProducto)))
            .ToListAsync(cancellationToken);

    public Task<bool> ExisteProducto(int idProducto, CancellationToken cancellationToken) =>
        db.Productos.AnyAsync(p => p.IdProducto == idProducto, cancellationToken);

    public Task<bool> ClienteComproProducto(int idCliente, int idProducto, CancellationToken cancellationToken) =>
        db.Ventas.AnyAsync(v => v.ClienteId == idCliente && v.Detalles.Any(d => d.ProductoId == idProducto), cancellationToken);

    public Task<bool> YaExisteResena(int idCliente, int idProducto, CancellationToken cancellationToken) =>
        db.Resenas.AnyAsync(r => r.IdCliente == idCliente && r.IdProducto == idProducto, cancellationToken);

    public async Task Agregar(Resena resena, CancellationToken cancellationToken)
    {
        db.Resenas.Add(resena);
        await db.SaveChangesAsync(cancellationToken);
    }
}
