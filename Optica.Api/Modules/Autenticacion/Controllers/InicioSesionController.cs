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
public sealed class InicioSesionController(InicioSesionService inicioSesion) : ControllerBase
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
                new Claim(AutenticacionClientesController.ClaimTipo, "cliente")
            }, AutenticacionClientesController.Esquema);
            await HttpContext.SignInAsync(AutenticacionClientesController.Esquema, new ClaimsPrincipal(identidad));
        }
        else
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                AccesoService.CrearPrincipal(resultado.Personal!));
        return Ok(resultado);
    }
}
