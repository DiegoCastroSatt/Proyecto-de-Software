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
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();
    }
}
