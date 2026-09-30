using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;

namespace Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AutenticacionController(
    IAutenticacionAdministradorService autenticacionAdministradorService) : ControllerBase
{
    [AllowAnonymous]
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

        var identidad = new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, administrador.IdAdministrador.ToString()),
            new Claim(ClaimTypes.Name, administrador.Nombre)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
        return Ok(administrador);
    }

    [HttpGet("admin/sesion")]
    public ActionResult<LoginAdministradorResponse> Sesion() => Ok(new LoginAdministradorResponse(
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity!.Name!));

    [HttpPost("admin/salir")]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}
