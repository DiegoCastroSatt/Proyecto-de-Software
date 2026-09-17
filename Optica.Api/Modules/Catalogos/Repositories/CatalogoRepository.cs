using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Catalogos.Models;

public class CatalogoRepository : ICatalogoRepository
{
    private readonly OpticaDbContext _context;

    public CatalogoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CatalogoItem>> ObtenerPorTipo(string tipo) =>
        await _context.Catalogos
            .Where(item => item.Tipo == tipo)
            .OrderBy(item => item.Nombre)
            .ToListAsync();

    public Task<CatalogoItem?> ObtenerPorNombre(string tipo, string nombre) =>
        _context.Catalogos.FirstOrDefaultAsync(item => item.Tipo == tipo && item.Nombre == nombre);

    public async Task<CatalogoItem> Crear(CatalogoItem item)
    {
        _context.Catalogos.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }
}
