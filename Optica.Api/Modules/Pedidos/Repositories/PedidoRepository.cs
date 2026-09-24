using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly OpticaDbContext _context;

    public PedidoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pedido>> ObtenerPedidosAsync()
    {
        return await _context.Pedidos
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync()
    {
        return await _context.Pedidos
            .Join(
                _context.Clientes,
                p => p.Rut,
                c => c.Rut,
                (p, c) => new PedidoResponseDto
                {
                    IdPedido = p.IdPedido,
                    NombreCliente = c.Nombre + " " + c.Apellido,
                    Fecha = p.Fecha,
                    Estado = p.Estado,
                    Total = p.Total,
                    Anotaciones = p.Anotaciones
                })
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync()
    {
        return await _context.Clientes
            .Where(c => c.Estado == "Activo")
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut)
    {
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.Rut == rut && c.Estado == "Activo");
    }

    public async Task<Pedido> CrearPedidoAsync(Pedido pedido)
    {
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id)
    {
        return await _context.Pedidos.FindAsync(id);
    }

    public async Task ActualizarPedidoAsync(Pedido pedido)
    {
        _context.Pedidos.Update(pedido);
        await _context.SaveChangesAsync();
    }
}
