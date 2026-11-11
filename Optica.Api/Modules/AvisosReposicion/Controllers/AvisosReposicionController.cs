using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.Controllers;
using Optica.Api.Modules.AvisosReposicion.Interfaces;
using Optica.Api.Modules.AvisosReposicion.Services;

namespace Optica.Api.Modules.AvisosReposicion.Controllers;

[Authorize(Policy = AutenticacionClientesController.Politica)]
[ApiController]
[Route("api/[controller]")]
public sealed class AvisosReposicionController(IAvisoReposicionService servicio) : ControllerBase
{
    [HttpPost("producto/{idProducto:int}")]
    public async Task<IActionResult> Crear(int idProducto, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();

        try
        {
            var aviso = await servicio.Crear(idCliente, idProducto, cancellationToken);
            return CreatedAtAction(nameof(MisSolicitudes), aviso);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (AvisoReposicionDuplicadoException ex)
        {
            return Conflict(new { mensaje = ex.Message });
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

    [HttpGet("mis-solicitudes")]
    public async Task<IActionResult> MisSolicitudes(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();

        return Ok(await servicio.ListarPorCliente(idCliente, cancellationToken));
    }
}
