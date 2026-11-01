using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Interfaces;
using Optica.Api.Modules.Resenas.Models;

namespace Optica.Api.Modules.Resenas.Repositories;

public sealed class ResenaRepository(OpticaDbContext db) : IResenaRepository
{
    public async Task<IReadOnlyList<ResenaResponseDto>> ListarPorProducto(int idProducto, CancellationToken cancellationToken) =>
        await db.Resenas.AsNoTracking().Where(r => r.IdProducto == idProducto)
            .OrderByDescending(r => r.FechaCreacionUtc)
            .Select(r => new ResenaResponseDto(r.IdResena, r.IdProducto, r.Calificacion, r.Comentario,
                r.Cliente.Nombre + " " + r.Cliente.Apellido.Substring(0, 1) + ".", r.FechaCreacionUtc, r.FechaActualizacionUtc))
            .ToListAsync(cancellationToken);

    public async Task<ResumenResenasDto> ObtenerResumen(int idProducto, CancellationToken cancellationToken)
    {
        var consulta = db.Resenas.AsNoTracking().Where(r => r.IdProducto == idProducto);
        var cantidad = await consulta.CountAsync(cancellationToken);
        var promedio = cantidad == 0 ? 0 : await consulta.AverageAsync(r => (double)r.Calificacion, cancellationToken);
        return new ResumenResenasDto(Math.Round(promedio, 1), cantidad);
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
