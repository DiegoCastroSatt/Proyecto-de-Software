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
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos. Si no tienes cuenta, regístrate; si acabas de crearla, confirma tu correo." });

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
                new
                {
                    mensaje = resultado.CorreoConfirmacionEnviado
                        ? "Cuenta creada. Revisa tu correo y confirma la cuenta antes de iniciar sesión. El enlace vence en 24 horas."
                        : "Cuenta creada, pero no se pudo enviar el correo de confirmación. Solicita que se reenvíe desde esta pantalla antes de iniciar sesión.",
                    correoConfirmacionEnviado = resultado.CorreoConfirmacionEnviado
                });
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

    [AllowAnonymous]
    [HttpPost("registro/confirmar")]
    public async Task<IActionResult> ConfirmarCuenta(
        [FromBody] ConfirmarCuentaClienteRequest request,
        CancellationToken cancellationToken)
    {
        var confirmada = await autenticacion.ConfirmarCuentaAsync(request.Token, cancellationToken);
        if (!confirmada)
            return BadRequest(new { mensaje = "El enlace no es válido, venció o ya fue utilizado. Solicita un nuevo correo de confirmación." });
        return Ok(new { mensaje = "Tu correo fue confirmado y la cuenta ya está activa. Ya puedes iniciar sesión." });
    }

    [AllowAnonymous]
    [HttpPost("registro/confirmacion/reenviar")]
    public async Task<IActionResult> ReenviarConfirmacion(
        [FromBody] ReenviarConfirmacionClienteRequest request,
        CancellationToken cancellationToken)
    {
        await autenticacion.ReenviarConfirmacionAsync(request.Correo, cancellationToken);
        return Accepted(new
        {
            mensaje = "Si existe una cuenta pendiente para ese correo, enviaremos un nuevo enlace. Revisa tu bandeja de entrada y spam."
        });
    }

    [AllowAnonymous]
    [HttpPost("recuperacion/solicitar")]
    public async Task<IActionResult> SolicitarRecuperacion(
        [FromBody] SolicitarRecuperacionClienteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var aceptada = await autenticacion.SolicitarRecuperacionAsync(
                request.Correo,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            if (!aceptada)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { mensaje = "Espera unos momentos antes de volver a solicitar un enlace." });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló la solicitud de recuperación de cliente ({TipoError}).", ex.GetType().Name);
        }

        // Respuesta genérica para mitigar la enumeración de cuentas
        return Accepted(new
        {
            mensaje = "Si el correo corresponde a una cuenta activa registrada, te enviaremos un enlace con las instrucciones para restablecer tu contraseña."
        });
    }

    [AllowAnonymous]
    [HttpPost("recuperacion/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacion(
        [FromBody] ConfirmarRecuperacionClienteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var confirmado = await autenticacion.ConfirmarRecuperacionAsync(
                request.Token,
                request.NuevaContrasena,
                request.ConfirmarContrasena,
                cancellationToken);

            if (!confirmado)
            {
                return BadRequest(new { mensaje = "El enlace no es válido, ha expirado o ya fue utilizado." });
            }

            return Ok(new { mensaje = "Tu contraseña ha sido actualizada correctamente. Ya puedes iniciar sesión." });
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
            logger.LogError("Falló la actualización de contraseña del cliente ({TipoError}).", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "No se pudo actualizar la contraseña. Inténtalo nuevamente más tarde." });
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
