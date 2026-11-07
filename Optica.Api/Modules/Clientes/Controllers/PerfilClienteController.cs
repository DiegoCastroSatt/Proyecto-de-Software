using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.Controllers;
using Optica.Api.Modules.Clientes.DTOs;
using Optica.Api.Modules.Clientes.Interfaces;

namespace Optica.Api.Modules.Clientes.Controllers;

[ApiController]
[Route("api/Clientes/perfil")]
[Authorize(Policy = AutenticacionClientesController.Politica)]
public sealed class PerfilClienteController(IClienteService clienteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PerfilClienteDto>> Obtener()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();
        var perfil = await clienteService.ObtenerPerfilAsync(idCliente);
        return perfil is null ? NotFound(new { mensaje = "No se encontró el perfil del cliente." }) : Ok(perfil);
    }

    [HttpPut]
    public async Task<ActionResult<PerfilClienteDto>> Actualizar(ActualizarPerfilClienteDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();
        try
        {
            return Ok(await clienteService.ActualizarPerfilAsync(idCliente, dto));
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
}
