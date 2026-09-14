using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.RegistroRecetas.Models;

public class RecetaRepository : IRecetaRepository
{
    private readonly OpticaDbContext _context;

    public RecetaRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExisteCliente(int clienteId)
    {
        return await _context.Clientes.AnyAsync(c => c.IdCliente == clienteId);
    }

    public async Task<Receta> Crear(Receta receta)
    {
        _context.Recetas.Add(receta);
        await _context.SaveChangesAsync();
        return receta;
    }

    public async Task<Receta?> ObtenerPorId(int id)
    {
        return await _context.Recetas
            .Include(r => r.Graduaciones)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}