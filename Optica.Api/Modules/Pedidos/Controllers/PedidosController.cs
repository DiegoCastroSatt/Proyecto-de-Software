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

    [HttpGet]
    public async Task<IActionResult> GetPedidos()
    {
        var pedidos = await _pedidoService.ObtenerPedidosAsync();
        return Ok(pedidos);
    }

    [HttpGet("clientes/buscar")]
    public async Task<IActionResult> BuscarClientesPorRut([FromQuery] string rut)
    {
        var clientes = await _pedidoService.BuscarClientesPorRutAsync(rut);
        return Ok(clientes);
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> GetClientes()
    {
        var clientes = await _pedidoService.ObtenerClientesActivosAsync();
        return Ok(clientes);
    }

    [HttpGet("cliente/{rut}")]
    public async Task<IActionResult> GetPedidosPorRut(string rut)
    {
        try
        {
            var pedidos = await _pedidoService.ObtenerPedidosPorRutAsync(rut);
            return Ok(pedidos);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPedidoById(int id)
    {
        var pedido = await _pedidoService.ObtenerPedidoPorIdAsync(id);
        if (pedido == null)
            return NotFound(new { mensaje = "Pedido no encontrado." });
        return Ok(pedido);
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
            var mensaje = result.EstadoNotificacion == "Fallida"
                ? "El pedido se actualizó correctamente, pero no se pudo enviar el correo al cliente."
                : "Estado actualizado exitosamente.";
            return Ok(new { mensaje, estado = result.Estado, estadoNotificacion = result.EstadoNotificacion });
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
