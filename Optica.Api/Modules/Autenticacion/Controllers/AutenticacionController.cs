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
    IAutenticacionAdministradorService autenticacionAdministradorService,
    ILogger<AutenticacionController> logger) : ControllerBase
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

    [AllowAnonymous]
    [HttpPost("admin/recuperacion/solicitar")]
    public async Task<IActionResult> SolicitarRecuperacionAdministrador(
        [FromBody] SolicitarRecuperacionAdministradorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var aceptada = await autenticacionAdministradorService.SolicitarRecuperacionAsync(
                request.NombreUsuario, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            if (!aceptada)
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { mensaje = "Espera antes de volver a solicitar un código." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló una solicitud de recuperación administrativa ({TipoError}).", ex.GetType().Name);
        }

        // La misma respuesta se devuelve si la cuenta no existe o si el envío falla.
        return Accepted(new
        {
            mensaje = "No podemos confirmar el resultado de la solicitud. Si recibiste un código en el correo asociado, ingrésalo aquí; si no, verifica el usuario e inténtalo más tarde."
        });
    }

    [AllowAnonymous]
    [HttpPost("admin/recuperacion/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacionAdministrador(
        [FromBody] ConfirmarRecuperacionAdministradorRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmado = await autenticacionAdministradorService.ConfirmarRecuperacionAsync(
                request.NombreUsuario, request.Codigo, request.NuevaContrasena,
                request.ConfirmarContrasena, cancellationToken);
            if (!confirmado)
                return BadRequest(new { mensaje = "El código no es válido, expiró o la solicitud ya no está disponible." });

            return Ok(new { mensaje = "La contraseña se actualizó correctamente. Ya puedes iniciar sesión." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló la actualización de contraseña administrativa ({TipoError}).", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "No se pudo actualizar la contraseña. El código sigue disponible para volver a intentarlo." });
        }
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
