using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Clientes.DTOs;
using Optica.Api.Modules.Clientes.Interfaces;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<IEnumerable<Cliente>>> Buscar([FromQuery] string? termino)
    {
        var lista = await _clienteService.BuscarAsync(termino);
        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CrearClienteDto dto)
    {
        try
        {
            var cliente = await _clienteService.RegistrarAsync(dto);
            return CreatedAtAction(nameof(Buscar), new { termino = cliente.Rut }, cliente);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarClienteDto dto)
    {
        try
        {
            var cliente = await _clienteService.ActualizarAsync(id, dto);
            return Ok(cliente);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al actualizar los datos en la base de datos.", detalle = ex.Message });
        }
    }

    [HttpPatch("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto)
    {
        try
        {
            var cliente = await _clienteService.CambiarEstadoAsync(id, dto.NuevoEstado);
            return Ok(new 
            { 
                mensaje = $"El cliente fue marcado como {dto.NuevoEstado.ToLower()} exitosamente.",
                idCliente = cliente.IdCliente,
                nuevoEstado = cliente.Estado
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al actualizar el estado en la base de datos.", detalle = ex.Message });
        }
    }
}