using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.RegistroRecetas.Models;

public class RecetaRepository : IRecetaRepository
{
    private readonly OpticaDbContext _context;

    public RecetaRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> BuscarClientePorRut(string rut)
    {
        var rutNormalizado = RutChilenoValidator.Normalizar(rut);

        return await _context.Clientes
            .Where(c => c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rutNormalizado)
            .OrderBy(c => c.IdCliente)
            .FirstOrDefaultAsync();
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

    public async Task<List<Cliente>> BuscarClientesPorRutParcial(string rutParcial)
    {
        var rutNormalizado = RutChilenoValidator.Normalizar(rutParcial);

        return await _context.Clientes
            .Where(c => EF.Functions.Like(
                c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper(),
                rutNormalizado + "%"))
            .OrderBy(c => c.Rut)
            .Take(8)
            .ToListAsync();
    }
}