using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.Controllers;
using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Interfaces;
using Optica.Api.Modules.Resenas.Services;

namespace Optica.Api.Modules.Resenas.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ResenasController(IResenaService resenas) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("producto/{idProducto:int}")]
    public async Task<IActionResult> ListarPorProducto(int idProducto, CancellationToken cancellationToken)
    {
        if (idProducto <= 0) return BadRequest(new { mensaje = "El producto no es válido." });
        var resumen = await resenas.ObtenerResumen(idProducto, cancellationToken);
        var items = await resenas.ListarPorProducto(idProducto, cancellationToken);
        return Ok(new { resumen, resenas = items });
    }

    [Authorize(Policy = AutenticacionClientesController.Politica)]
    [HttpGet("mis-productos")]
    public async Task<IActionResult> MisProductos(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente)) return Unauthorized();
        return Ok(await resenas.ListarProductosComprados(idCliente, cancellationToken));
    }

    [Authorize(Policy = AutenticacionClientesController.Politica)]
    [HttpPost("producto/{idProducto:int}")]
    public async Task<IActionResult> Crear(int idProducto, [FromBody] CrearResenaDto dto, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente)) return Unauthorized();
        try
        {
            var creada = await resenas.Crear(idCliente, idProducto, dto, cancellationToken);
            return CreatedAtAction(nameof(ListarPorProducto), new { idProducto }, creada with { NombreCliente = ObtenerNombrePublico() });
        }
        catch (ResenaDuplicadaException ex) { return Conflict(new { mensaje = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    private string ObtenerNombrePublico()
    {
        var partes = (User.FindFirstValue(ClaimTypes.Name) ?? "Cliente")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return partes.Length > 1 ? $"{partes[0]} {partes[^1][0]}." : partes.FirstOrDefault() ?? "Cliente";
    }
}
