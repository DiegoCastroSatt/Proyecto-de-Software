using Optica.Api.Modules.Accesos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Interfaces;
using Optica.Api.Modules.Autenticacion.Services;

namespace Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AutenticacionController(
    IAutenticacionAdministradorService autenticacionAdministradorService,
    ILogger<AutenticacionController> logger,
    AccesoService acceso,
    RecuperacionService recuperacion) : ControllerBase
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
            cancellationToken) ?? await acceso.IniciarTrabajadorAsync(request, cancellationToken);

        if (administrador is null)
        {
            return Unauthorized(new { mensaje = "Nombre de usuario o contraseña incorrectos." });
        }

        await HttpContext.SignOutAsync(AutenticacionClientesController.Esquema);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccesoService.CrearPrincipal(administrador));
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
            var aceptada = await recuperacion.SolicitarAsync(
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
            var confirmado = await recuperacion.ConfirmarAsync(
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

    [Authorize(Policy = PermisosAcceso.Personal)]
    [HttpGet("admin/sesion")]
    public async Task<ActionResult<LoginAdministradorResponse>> Sesion(CancellationToken ct)
    {
        var sesion = await acceso.ObtenerSesionAsync(User, ct);
        return sesion is null ? Unauthorized() : Ok(sesion);
    }

    [Authorize(Policy = PermisosAcceso.Personal)]
    [HttpPost("admin/salir")]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
}
