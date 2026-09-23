using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly OpticaDbContext _context;

    public PedidosController(OpticaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Devuelve la lista de pedidos con el nombre completo del cliente y el estado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPedidos()
    {
        var pedidos = await _context.Pedidos
            .Join(
                _context.Clientes,
                p => p.IdCliente,
                c => c.IdCliente,
                (p, c) => new
                {
                    p.IdPedido,
                    NombreCliente = c.Nombre + " " + c.Apellido,
                    p.Fecha,
                    p.Estado,
                    p.Total,
                    p.Anotaciones
                })
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();

        return Ok(pedidos);
    }

    /// <summary>
    /// Devuelve la lista de clientes activos (id, nombre completo).
    /// </summary>
    [HttpGet("clientes")]
    public async Task<IActionResult> GetClientes()
    {
        var clientes = await _context.Clientes
            .Where(c => c.Estado == "Activo")
            .OrderBy(c => c.Nombre)
            .Select(c => new
            {
                c.IdCliente,
                NombreCompleto = c.Nombre + " " + c.Apellido
            })
            .ToListAsync();

        return Ok(clientes);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePedido([FromBody] CreatePedidoDto dto)
    {
        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Rut == dto.Rut && c.Estado == "Activo");

        if (cliente == null)
        {
            return BadRequest(new { mensaje = "No se encontró un cliente activo con ese RUT." });
        }

        var pedido = new Pedido
        {
            IdCliente = cliente.IdCliente,
            Fecha = dto.Fecha,
            Anotaciones = dto.Anotaciones,
            Estado = dto.Estado,
            Total = dto.Total
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPedidos), new { id = pedido.IdPedido }, new
        {
            pedido.IdPedido,
            NombreCliente = cliente.Nombre + " " + cliente.Apellido,
            pedido.Fecha,
            pedido.Estado,
            pedido.Total,
            pedido.Anotaciones
        });
    }
}
