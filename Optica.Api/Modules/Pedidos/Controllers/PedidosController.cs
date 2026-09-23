using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;

namespace Optica.Api.Modules.Pedidos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidosController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    /// <summary>
    /// Devuelve la lista de pedidos con el nombre completo del cliente y el estado.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPedidos()
    {
        var pedidos = await _pedidoService.ObtenerPedidosAsync();
        return Ok(pedidos);
    }

    /// <summary>
    /// Devuelve la lista de clientes activos (id, nombre completo).
    /// </summary>
    [HttpGet("clientes")]
    public async Task<IActionResult> GetClientes()
    {
        var clientes = await _pedidoService.ObtenerClientesActivosAsync();
        return Ok(clientes);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePedido([FromBody] CreatePedidoDto dto)
    {
        try
        {
            var result = await _pedidoService.CrearPedidoAsync(dto);
            return CreatedAtAction(nameof(GetPedidos), new { id = result.IdPedido }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateEstadoPedidoDto dto)
    {
        try
        {
            var result = await _pedidoService.ActualizarEstadoAsync(id, dto);
            return Ok(new { mensaje = "Estado actualizado exitosamente.", estado = result.Estado });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
