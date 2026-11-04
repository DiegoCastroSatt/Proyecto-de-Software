using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Productos.DTOs;
using Optica.Api.Modules.Productos.Models;

public class ProductoRepository : IProductoRepository
{
    private readonly OpticaDbContext _context;

    public ProductoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<Producto> CrearConHistorialStock(Producto producto, int idResponsable)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        _context.HistorialStock.AddRange(
            new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = "Registro de nuevo producto",
                TipoStock = "Normal",
                ValorAnterior = 0,
                ValorActual = producto.Stock
            },
            new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = "Registro de nuevo producto",
                TipoStock = "Minimo",
                ValorAnterior = 0,
                ValorActual = producto.StockMinimo
            });
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        return producto;
    }

    public Task<bool> ExisteCodigo(string codigo) =>
        _context.Productos.AnyAsync(producto => producto.Codigo == codigo);

    public async Task<List<Producto>> Buscar(string termino)
    {
        var consulta = _context.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            var filtro = termino.Trim();
            consulta = consulta.Where(producto => producto.Nombre.Contains(filtro) || producto.Categoria.Contains(filtro) || producto.Codigo.Contains(filtro));
        }

        var productos = await consulta.OrderBy(producto => producto.Nombre).ToListAsync();
        await MarcarProductosConVentas(productos);
        return productos;
    }

    public async Task<Producto?> ObtenerPorId(int id)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(item => item.IdProducto == id);
        if (producto is not null)
        {
            await MarcarProductosConVentas([producto]);
        }

        return producto;
    }

    private async Task MarcarProductosConVentas(List<Producto> productos)
    {
        var ids = productos.Select(producto => producto.IdProducto).ToList();
        var idsConVentas = await _context.DetallesVenta
            .Where(detalle => detalle.ProductoId.HasValue && ids.Contains(detalle.ProductoId.Value))
            .Select(detalle => detalle.ProductoId!.Value)
            .Distinct()
            .ToListAsync();

        foreach (var producto in productos)
        {
            producto.TieneVentas = idsConVentas.Contains(producto.IdProducto);
        }
    }

    public async Task<Producto> ActualizarConHistorialStock(Producto producto, int idResponsable, string motivoMovimiento)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        var productosBloqueados = await _context.Productos
            .FromSqlInterpolated($"SELECT * FROM productos WHERE id_producto = {producto.IdProducto} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();
        var valoresAnteriores = productosBloqueados.SingleOrDefault()
            ?? throw new KeyNotFoundException("No se encuentra el producto.");

        var stockCambio = valoresAnteriores.Stock != producto.Stock;
        var stockMinimoCambio = valoresAnteriores.StockMinimo != producto.StockMinimo;
        if ((stockCambio || stockMinimoCambio) && string.IsNullOrWhiteSpace(motivoMovimiento))
        {
            throw new ArgumentException("El motivo es obligatorio cuando cambia el stock.");
        }

        _context.Entry(producto).OriginalValues.SetValues(valoresAnteriores);
        _context.Productos.Update(producto);

        if (stockCambio)
        {
            _context.HistorialStock.Add(new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = motivoMovimiento,
                TipoStock = "Normal",
                ValorAnterior = valoresAnteriores.Stock,
                ValorActual = producto.Stock
            });
        }

        if (stockMinimoCambio)
        {
            _context.HistorialStock.Add(new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = motivoMovimiento,
                TipoStock = "Minimo",
                ValorAnterior = valoresAnteriores.StockMinimo,
                ValorActual = producto.StockMinimo
            });
        }

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        await MarcarProductosConVentas([producto]);
        return producto;
    }

    public async Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(
        int idProducto,
        int pagina,
        int tamanoPagina)
    {
        if (!await _context.Productos.AsNoTracking().AnyAsync(producto => producto.IdProducto == idProducto))
        {
            return null;
        }

        var consulta = _context.HistorialStock.AsNoTracking()
            .Where(historial => historial.IdProducto == idProducto);
        var totalRegistros = await consulta.CountAsync();
        var movimientos = await (
            from historial in consulta
            join responsable in _context.Trabajadores.AsNoTracking()
                on historial.IdResponsable equals responsable.IdTrabajador
            orderby historial.FechaCambio descending, historial.IdCambio descending
            select new HistorialStockMovimientoResponseDto(
                historial.IdCambio,
                historial.IdProducto,
                historial.MotivoMovimiento,
                historial.IdResponsable,
                responsable.Nombre,
                historial.TipoStock,
                historial.ValorAnterior,
                historial.ValorActual,
                historial.FechaCambio))
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new HistorialStockPaginadoResponseDto(
            pagina,
            tamanoPagina,
            totalRegistros,
            (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            movimientos);
    }

    public async Task<bool> Eliminar(int id)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(item => item.IdProducto == id);
        if (producto is null)
        {
            return false;
        }

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return true;
    }
}