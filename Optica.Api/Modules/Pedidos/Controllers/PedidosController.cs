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
        // Buscar el cliente por nombre completo (nombre + apellido)
        var partes = dto.NombreCliente.Trim().Split(' ', 2);
        var nombre = partes[0];
        var apellido = partes.Length > 1 ? partes[1] : "";

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c =>
                c.Nombre == nombre && c.Apellido == apellido && c.Estado == "Activo");

        // Si no se encuentra por nombre+apellido, buscar solo por coincidencia parcial
        if (cliente == null)
        {
            cliente = await _context.Clientes
                .FirstOrDefaultAsync(c =>
                    (c.Nombre + " " + c.Apellido) == dto.NombreCliente.Trim()
                    && c.Estado == "Activo");
        }

        if (cliente == null)
        {
            return BadRequest(new { mensaje = "No se encontró un cliente activo con ese nombre." });
        }

        var pedido = new Pedido
        {
            IdCliente = cliente.IdCliente,
            Fecha = dto.Fecha,
            Anotaciones = dto.Anotaciones,
            Estado = "Pendiente"
        };

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPedidos), new { id = pedido.IdPedido }, new
        {
            pedido.IdPedido,
            NombreCliente = cliente.Nombre + " " + cliente.Apellido,
            pedido.Fecha,
            pedido.Estado,
            pedido.Anotaciones
        });
    }
}
