using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Services;

namespace Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/Autenticacion/cliente")]
public sealed class AutenticacionClientesController(
    AutenticacionClienteService autenticacion,
    OpticaDbContext db,
    ILogger<AutenticacionClientesController> logger) : ControllerBase
{
    public const string Esquema = "OpticaCliente";
    public const string Politica = "ClienteAutenticado";
    public const string ClaimTipo = "tipo_usuario";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ClienteSesionResponse>> IniciarSesion(
        LoginClienteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (sesion, credencialesValidas) = await autenticacion.IniciarSesionAsync(request, cancellationToken);
            if (!credencialesValidas || sesion is null)
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos. Si no tienes cuenta, regístrate. Si tus datos ya están registrados en el centro, contacta al personal para verificar o activar tu acceso." });

            var identidad = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, sesion.IdCliente.ToString()),
                new Claim(ClaimTypes.Name, sesion.Nombre),
                new Claim(ClaimTypes.Email, sesion.Correo),
                new Claim(ClaimTipo, "cliente")
            }, Esquema);
            await HttpContext.SignInAsync(Esquema, new ClaimsPrincipal(identidad));
            return Ok(sesion);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló el inicio de sesión de cliente ({TipoError}).", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "No se pudo iniciar sesión. Inténtalo nuevamente más tarde." });
        }
    }

    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(
        RegistroClienteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await autenticacion.RegistrarAsync(request, cancellationToken);
            if (resultado.Error is not null)
                return BadRequest(new { mensaje = resultado.Error });
            if (resultado.ConflictoExistente)
                return Conflict(new { mensaje = "Ya existe un registro con el RUT o correo indicado. Por seguridad no vinculamos cuentas automáticamente; contacta al personal para verificar o activar tu acceso." });
            if (!resultado.Registrado)
                return BadRequest(new { mensaje = "No se pudo completar el registro." });

            return StatusCode(StatusCodes.Status201Created,
                new { mensaje = "Tu cuenta fue creada correctamente. Ya puedes iniciar sesión." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló el registro de cliente ({TipoError}).", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "No se pudo crear la cuenta. Inténtalo nuevamente más tarde." });
        }
    }

    [Authorize(Policy = Politica)]
    [HttpGet("sesion")]
    public async Task<ActionResult<ClienteSesionResponse>> Sesion(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();
        var cuenta = await db.CuentasCliente.Include(c => c.Cliente)
            .SingleOrDefaultAsync(c => c.IdCliente == idCliente && c.Estado == "Activa"
                && c.Cliente!.Estado == "Activo", cancellationToken);
        if (cuenta?.Cliente is null)
            return Unauthorized();
        return Ok(AutenticacionClienteService.CrearSesion(cuenta));
    }

    [Authorize(Policy = Politica)]
    [HttpPost("salir")]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(Esquema);
        return NoContent();
    }
}
