using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
namespace Optica.Api.Modules.Accesos;

[ApiController]
[Route("api/CuentaTrabajador")]
[Authorize(Policy = PermisosAcceso.SoloTrabajador)]
public sealed class CuentaTrabajadorController(OpticaDbContext db, IPasswordHasher<Trabajador> hasher) : ControllerBase
{
    [HttpPost("contrasena")]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaTrabajadorRequest request, CancellationToken ct)
    {
        if (!Optica.Api.Modules.Autenticacion.Services.AutenticacionClienteService.ContrasenaValida(request.NuevaContrasena))
            return BadRequest(new { mensaje = "La contraseña debe tener al menos 8 caracteres e incluir letras, números y un símbolo." });
        // La identidad se toma exclusivamente de la sesión, nunca de un identificador del formulario.
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Unauthorized();
        var trabajador = await db.Trabajadores.SingleOrDefaultAsync(t => t.IdTrabajador == id && t.Estado == "Activo", ct);
        if (trabajador is null) return Unauthorized();
        if (hasher.VerifyHashedPassword(trabajador, trabajador.Contrasena, request.ContrasenaActual) == PasswordVerificationResult.Failed)
            return BadRequest(new { mensaje = "La contraseña actual es incorrecta." });
        if (request.NuevaContrasena == request.ContrasenaActual)
            return BadRequest(new { mensaje = "Elige una contraseña distinta de la actual." });
        trabajador.Contrasena = hasher.HashPassword(trabajador, request.NuevaContrasena);
        await db.DesafiosRecuperacion.Where(d => d.IdPersonal == trabajador.IdTrabajador && d.Estado == "Pendiente")
            .ExecuteUpdateAsync(u => u.SetProperty(d => d.Estado, "Invalidado"), ct);
        await db.SaveChangesAsync(ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { mensaje = "Contraseña actualizada. Inicia sesión con tu nueva contraseña." });
    }
}
public sealed record CambiarContrasenaTrabajadorRequest
{
    [Required, StringLength(255)] public required string ContrasenaActual { get; init; }
    [Required, StringLength(255, MinimumLength = 8)] public required string NuevaContrasena { get; init; }
    [Required, StringLength(255), Compare(nameof(NuevaContrasena))] public required string ConfirmarContrasena { get; init; }
}
