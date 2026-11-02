using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.DTOs;
using Optica.Api.Modules.Autenticacion.Services;

namespace Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/Autenticacion/login")]
public sealed class InicioSesionController(InicioSesionService inicioSesion, RecuperacionService recuperacion) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<ResultadoInicioSesion>> Iniciar(LoginClienteRequest request, CancellationToken ct)
    {
        var resultado = await inicioSesion.IniciarAsync(request, ct);
        if (resultado is null) return Unauthorized(new { mensaje = "Correo, usuario o contraseña incorrectos." });
        // Una nueva sesión sustituye a cualquier identidad anterior del mismo navegador.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(AutenticacionClientesController.Esquema);
        if (resultado.Cliente is { } cliente)
        {
            var identidad = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, cliente.IdCliente.ToString()),
                new Claim(ClaimTypes.Name, cliente.Nombre),
                new Claim(ClaimTypes.Email, cliente.Correo),
                new Claim(PermisosAcceso.VersionCredencial, cliente.VersionCredencial),
                new Claim(AutenticacionClientesController.ClaimTipo, "cliente")
            }, AutenticacionClientesController.Esquema);
            await HttpContext.SignInAsync(AutenticacionClientesController.Esquema, new ClaimsPrincipal(identidad));
        }
        else
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                AccesoService.CrearPrincipal(resultado.Personal!));
        return Ok(resultado);
    }

    [AllowAnonymous]
    [HttpPost("/api/Autenticacion/recuperacion/solicitar")]
    public async Task<IActionResult> SolicitarRecuperacion(SolicitarRecuperacionRequest request, CancellationToken ct)
    {
        try
        {
            await recuperacion.SolicitarAsync(request.Identificador, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<InicioSesionController>>()
                .LogError("Falló una solicitud de recuperación ({TipoError}).", ex.GetType().Name);
        }
        return Accepted(new { mensaje = "Si los datos corresponden a una cuenta activa, enviaremos un código al correo registrado." });
    }

    [AllowAnonymous]
    [HttpPost("/api/Autenticacion/recuperacion/confirmar")]
    public async Task<IActionResult> ConfirmarRecuperacion(ConfirmarRecuperacionRequest request, CancellationToken ct)
    {
        try
        {
            var correcto = await recuperacion.ConfirmarAsync(request.Identificador, request.Codigo,
                request.NuevaContrasena, request.ConfirmarContrasena, ct);
            return correcto ? Ok(new { mensaje = "La contraseña se actualizó correctamente. Ya puedes iniciar sesión." })
                : BadRequest(new { mensaje = "El código no es válido, expiró o ya no está disponible." });
        }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<InicioSesionController>>()
                .LogError("Falló la actualización de contraseña ({TipoError}).", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "No se pudo actualizar la contraseña." });
        }
    }
}
