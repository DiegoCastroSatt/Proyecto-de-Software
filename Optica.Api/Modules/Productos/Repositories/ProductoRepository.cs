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

    public Task<List<Producto>> Buscar(string termino)
    {
        var consulta = _context.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            var filtro = termino.Trim();
            consulta = consulta.Where(producto => producto.Nombre.Contains(filtro) || producto.Categoria.Contains(filtro) || producto.Codigo.Contains(filtro));
        }

        return consulta.OrderBy(producto => producto.Nombre).ToListAsync();
    }

    public Task<Producto?> ObtenerPorId(int id) =>
        _context.Productos.FirstOrDefaultAsync(producto => producto.IdProducto == id);

    public async Task<Producto> Actualizar(Producto producto)
    {
        _context.Productos.Update(producto);
        await _context.SaveChangesAsync();
        return producto;
    }
}