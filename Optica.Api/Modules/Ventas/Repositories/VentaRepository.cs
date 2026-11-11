using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.Ventas.Repositories;

public class VentaRepository(OpticaDbContext db) : IVentaRepository
{
    public async Task<IReadOnlyList<VentaResponseDto>> Listar() => await db.Ventas.AsNoTracking()
        .OrderByDescending(v => v.Fecha)
        .Select(v => new VentaResponseDto(v.IdVenta, v.Fecha, v.Total,
            v.Detalles.Select(d => new ProductoHistorialDto(d.ProductoId,
                d.NombreProducto,
                d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList(),
            v.Cliente == null ? null : v.Cliente.Rut,
            v.Cliente == null ? null : v.Cliente.Nombre + " " + v.Cliente.Apellido))
        .ToListAsync();

    public Task<int?> BuscarClienteActivo(string rutNormalizado) => db.Clientes.AsNoTracking()
        .Where(c => c.Estado == "Activo" && c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rutNormalizado)
        .Select(c => (int?)c.IdCliente).SingleOrDefaultAsync();

    public async Task Guardar(Venta venta, int idResponsable)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();
        foreach (var detalle in venta.Detalles.OrderBy(d => d.ProductoId))
        {
            var productosBloqueados = await db.Productos
                .FromSqlInterpolated($"SELECT * FROM productos WHERE id_producto = {detalle.ProductoId} FOR UPDATE")
                .AsNoTracking()
                .ToListAsync();
            var producto = productosBloqueados.SingleOrDefault();
            if (producto is null || producto.Stock < detalle.Cantidad)
            {
                throw new ArgumentException(producto is null || producto.Stock <= 0
                    ? $"{detalle.NombreProducto}: Sin productos en stock."
                    : $"{detalle.NombreProducto}: stock insuficiente. Disponibles: {producto.Stock}.");
            }

            var stockAnterior = producto.Stock;
            var stockActual = stockAnterior - detalle.Cantidad;
            var productoActualizacion = db.Productos
                .Where(p => p.IdProducto == detalle.ProductoId && p.Stock == stockAnterior);
            var actualizados = stockActual == 0
                ? await productoActualizacion.ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Stock, stockActual)
                    .SetProperty(p => p.Estado, "Agotado"))
                : await productoActualizacion.ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Stock, stockActual));
            if (actualizados != 1)
                throw new InvalidOperationException("No se pudo actualizar el stock del producto.");

            db.HistorialStock.Add(new HistorialStock
            {
                IdProducto = detalle.ProductoId!.Value,
                IdResponsable = idResponsable,
                MotivoMovimiento = "Venta",
                TipoStock = "Normal",
                ValorAnterior = stockAnterior,
                ValorActual = stockActual
            });
        }

        db.Ventas.Add(venta);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
    }
}
