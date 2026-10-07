using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Interfaces;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.DTOs;

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

    public async Task<HistorialClienteDto?> ObtenerHistorialAsync(int idCliente)
{
    var cliente = await _context.Clientes.FindAsync(idCliente);
    if (cliente == null)
    {
        return null;
    }

    // 1. Obtener recetas con sus graduaciones
    var recetas = await _context.Recetas
        .Include(r => r.Graduaciones)
        .Where(r => r.ClienteId == idCliente)
        .OrderByDescending(r => r.Fecha)
        .Select(r => new HistorialRecetaDto
        {
            IdReceta = r.Id,
            Fecha = r.Fecha,
            Observaciones = r.Observaciones,
            ImagenPath = r.ImagenPath,
            Graduaciones = r.Graduaciones.Select(g => new HistorialGraduacionDto
            {
                Ojo = g.Ojo,
                Esfera = g.Esfera,
                Cilindro = g.Cilindro,
                Eje = g.Eje,
                Adicion = g.Adicion
            }).ToList()
        })
        .ToListAsync();

    // 2. Obtener pedidos asociados al RUT del cliente (normalizando para comparar limpiamente)
    string rutCliente = cliente.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper();
    var pedidos = await _context.Pedidos
        .Where(p => p.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rutCliente)
        .OrderByDescending(p => p.Fecha)
        .Select(p => new HistorialPedidoDto
        {
            IdPedido = p.IdPedido,
            IdReceta = p.IdReceta,
            Fecha = p.Fecha,
            Estado = p.Estado,
            Total = p.Total,
            Anotaciones = p.Anotaciones
        })
        .ToListAsync();

    return new HistorialClienteDto
    {
        IdCliente = cliente.IdCliente,
        Rut = cliente.Rut,
        NombreCompleto = $"{cliente.Nombre} {cliente.Apellido}".Trim(),
        Telefono = cliente.Telefono,
        Correo = cliente.Correo,
        Estado = cliente.Estado,
        Recetas = recetas,
        Pedidos = pedidos
    };
}
}