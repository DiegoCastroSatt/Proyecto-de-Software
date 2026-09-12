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
}