using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AutenticacionController(
    IAutenticacionAdministradorService autenticacionAdministradorService) : ControllerBase
{
    [HttpPost("admin")]
    [ProducesResponseType(typeof(LoginAdministradorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginAdministradorResponse>> IniciarSesionAdministrador(
        [FromBody] LoginAdministradorRequest request,
        CancellationToken cancellationToken)
    {
        var administrador = await autenticacionAdministradorService.IniciarSesionAsync(
            request,
            cancellationToken);

        if (administrador is null)
        {
            return Unauthorized(new { mensaje = "Nombre de usuario o contraseña incorrectos." });
        }

        return Ok(administrador);
    }
}