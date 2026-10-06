using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Interfaces;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly OpticaDbContext _context;

    public ClienteRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Cliente>> BuscarAsync(string? termino)
    {
        var query = _context.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            string t = termino.Trim().ToLower();
            query = query.Where(c =>
                c.Rut.ToLower().Contains(t) ||
                c.Nombre.ToLower().Contains(t) ||
                c.Apellido.ToLower().Contains(t)
            );
        }

        return await query
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        return await _context.Clientes.FindAsync(id);
    }

    public async Task<Cliente?> ObtenerPorRutAsync(string rut)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Rut == rut);
    }

    public async Task<bool> ExisteRutAsync(string rut)
    {
        return await _context.Clientes.AnyAsync(c => c.Rut == rut);
    }

    public async Task<Cliente> CrearAsync(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return cliente;
    }

    public async Task ActualizarAsync(Cliente cliente)
    {
        await _context.SaveChangesAsync();
    }
}