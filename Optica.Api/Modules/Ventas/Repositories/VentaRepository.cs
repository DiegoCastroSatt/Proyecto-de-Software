using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Repositories;

public class VentaRepository(OpticaDbContext db) : IVentaRepository
{
    public async Task<IReadOnlyList<VentaResponseDto>> Listar() => await db.Ventas.AsNoTracking()
        .OrderByDescending(v => v.Fecha)
        .Select(v => new VentaResponseDto(v.IdVenta, v.Fecha, v.Total,
            v.Detalles.Select(d => new ProductoHistorialDto(d.ProductoId,
                d.NombreProducto,
                d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList()))
        .ToListAsync();

    public async Task Guardar(Venta venta)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();
        // El descuento condicional impide sobreventas entre solicitudes simultáneas.
        foreach (var detalle in venta.Detalles.OrderBy(d => d.ProductoId))
        {
            var actualizados = await db.Productos
                .Where(p => p.IdProducto == detalle.ProductoId && p.Stock >= detalle.Cantidad)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Stock, p => p.Stock - detalle.Cantidad));
            if (actualizados == 0)
            {
                var stock = await db.Productos.AsNoTracking()
                    .Where(p => p.IdProducto == detalle.ProductoId)
                    .Select(p => (int?)p.Stock).SingleOrDefaultAsync();
                throw new ArgumentException(stock is null or <= 0
                    ? $"{detalle.NombreProducto}: Sin productos en stock."
                    : $"{detalle.NombreProducto}: stock insuficiente. Disponibles: {stock}.");
            }
            await db.Productos.Where(p => p.IdProducto == detalle.ProductoId && p.Stock == 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Estado, "Agotado"));
        }
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
    }
}
