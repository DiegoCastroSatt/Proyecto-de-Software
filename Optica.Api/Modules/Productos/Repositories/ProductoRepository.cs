using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Productos.Models;

public class ProductoRepository : IProductoRepository
{
    private readonly OpticaDbContext _context;

    public ProductoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<Producto> Crear(Producto producto)
    {
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();
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
            .Where(detalle => ids.Contains(detalle.ProductoId))
            .Select(detalle => detalle.ProductoId)
            .Distinct()
            .ToListAsync();

        foreach (var producto in productos)
        {
            producto.TieneVentas = idsConVentas.Contains(producto.IdProducto);
        }
    }

    public async Task<Producto> Actualizar(Producto producto)
    {
        _context.Productos.Update(producto);
        await _context.SaveChangesAsync();
        await MarcarProductosConVentas([producto]);
        return producto;
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