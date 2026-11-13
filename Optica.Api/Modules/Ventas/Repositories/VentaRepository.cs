using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.Ventas.Repositories;

public class VentaRepository(OpticaDbContext db) : IVentaRepository
{
    public async Task<IReadOnlyList<VentaResponseDto>> Listar() =>
        await Proyectar(db.Ventas.AsNoTracking().OrderByDescending(v => v.Fecha).ThenByDescending(v => v.IdVenta)).ToListAsync();

    public async Task<VentasPaginaDto> ListarPagina(int desplazamiento, int tamano)
    {
        var consulta = db.Ventas.AsNoTracking();
        var ventas = await Proyectar(consulta.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.IdVenta)
            .Skip(desplazamiento).Take(tamano)).ToListAsync();
        var total = await consulta.CountAsync();
        var totalPaginas = (int)Math.Ceiling(total / (double)tamano);
        return new VentasPaginaDto(ventas, desplazamiento / tamano + 1 < totalPaginas, totalPaginas);
    }

    public async Task<VentasResumenDto> Resumir(DateTime desde, DateTime hasta, DateTime desdeRanking, DateTime hastaRanking, int limite)
    {
        var consulta = db.Ventas.AsNoTracking();
        var total = await consulta.CountAsync();
        var meses = await consulta.Where(v => v.Fecha >= desde && v.Fecha < hasta)
            .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
            .Select(g => new VentasMesDto(g.Key.Year, g.Key.Month, g.Sum(v => v.Total), g.Count()))
            .ToListAsync();
        var ranking = await consulta.Where(v => v.Fecha >= desdeRanking && v.Fecha <= hastaRanking)
            .SelectMany(v => v.Detalles)
            .GroupBy(d => new { d.ProductoId, Nombre = d.ProductoId == null ? d.NombreProducto : "" })
            .OrderByDescending(g => g.Sum(d => d.Cantidad))
            .ThenBy(g => g.Key.ProductoId).ThenBy(g => g.Key.Nombre)
            .Select(g => new VentaRankingDto(g.Key.ProductoId ?? 0, g.Max(d => d.NombreProducto) ?? "Producto eliminado", g.Sum(d => d.Cantidad)))
            .Take(limite).ToListAsync();
        return new VentasResumenDto(total, meses, ranking);
    }

    private static IQueryable<VentaResponseDto> Proyectar(IQueryable<Venta> consulta) => consulta
        .Select(v => new VentaResponseDto(v.IdVenta, v.Fecha, v.Total,
            v.Detalles.Select(d => new ProductoHistorialDto(d.ProductoId,
                d.NombreProducto,
                d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList(),
            v.Cliente == null ? null : v.Cliente.Rut,
            v.Cliente == null ? null : v.Cliente.Nombre + " " + v.Cliente.Apellido));

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
