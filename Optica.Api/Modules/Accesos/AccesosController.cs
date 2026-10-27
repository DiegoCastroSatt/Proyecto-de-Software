using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
namespace Optica.Api.Modules.Accesos;

[ApiController]
[Route("api/Accesos")]
[Authorize(Policy = PermisosAcceso.SoloAdministrador)]
public sealed class AccesosController(OpticaDbContext db, IPasswordHasher<Trabajador> hasher) : ControllerBase
{
    [HttpGet("permisos")]
    public IActionResult Permisos() => Ok(PermisosAcceso.Catalogo.Select(p => new { codigo = p.Key, nombre = p.Value }));
    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok((await db.RolesTrabajadores.AsNoTracking().OrderBy(r => r.Nombre).ToListAsync(ct))
        .Select(r => new { r.IdRol, r.Nombre, permisos = AccesoService.LeerPermisos(r.PermisosJson) }));
    [HttpPost("roles")]
    public Task<IActionResult> CrearRol(RolRequest request, CancellationToken ct) => GuardarRol(null, request, ct);
    [HttpPut("roles/{id:int}")]
    public Task<IActionResult> EditarRol(int id, RolRequest request, CancellationToken ct) => GuardarRol(id, request, ct);
    private async Task<IActionResult> GuardarRol(int? id, RolRequest request, CancellationToken ct)
    {
        if (request.Permisos.Any(p => p is null || !PermisosAcceso.Catalogo.ContainsKey(p))) return BadRequest(new { mensaje = "El rol contiene un permiso desconocido." });
        var nombre = request.Nombre.Trim();
        if (nombre.Length == 0) return BadRequest(new { mensaje = "Ingresa un nombre para el rol." });
        if (await db.RolesTrabajadores.AnyAsync(r => r.Nombre == nombre && r.IdRol != (id ?? 0), ct)) return Conflict(new { mensaje = "Ya existe un rol con ese nombre." });
        var rol = id.HasValue ? await db.RolesTrabajadores.FindAsync([id.Value], ct) : new RolTrabajador();
        if (rol is null) return NotFound();
        rol.Nombre = nombre; rol.PermisosJson = JsonSerializer.Serialize(request.Permisos.Distinct().ToArray());
        if (!id.HasValue) db.RolesTrabajadores.Add(rol);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { mensaje = "No se pudo guardar el rol. Revisa si el nombre ya está en uso." }); }
        return Ok(new { rol.IdRol, rol.Nombre, permisos = AccesoService.LeerPermisos(rol.PermisosJson) });
    }
    [HttpGet("trabajadores")]
    public async Task<IActionResult> Trabajadores(CancellationToken ct) => Ok(await db.Trabajadores.AsNoTracking().OrderBy(t => t.Nombre)
        .Select(t => new { t.IdTrabajador, t.Nombre, t.Correo, t.Estado, t.IdRol, rol = t.Rol.Nombre, tipoUsuario = "Trabajador" }).ToListAsync(ct));
    [HttpPost("trabajadores")]
    public async Task<IActionResult> CrearTrabajador(CrearTrabajadorRequest request, CancellationToken ct)
    {
        var correo = request.Correo.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(request.Nombre)) return BadRequest(new { mensaje = "Ingresa el nombre del trabajador." });
        if (!await db.RolesTrabajadores.AnyAsync(r => r.IdRol == request.IdRol, ct)) return BadRequest(new { mensaje = "Selecciona un rol existente." });
        if (await db.Trabajadores.AnyAsync(t => t.Correo == correo, ct) || await db.Administradores.AnyAsync(a => a.Nombre.ToUpper() == correo, ct))
            return Conflict(new { mensaje = "Ese correo ya está en uso como usuario." });
        var t = new Trabajador { Nombre = request.Nombre.Trim(), Correo = correo, IdRol = request.IdRol,
            IdAdministradorCreador = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) };
        t.Contrasena = hasher.HashPassword(t, request.Contrasena);
        db.Trabajadores.Add(t);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { mensaje = "No se pudo crear el trabajador. Revisa si el correo ya está en uso." }); }
        return Ok(new { t.IdTrabajador });
    }
    [HttpPut("trabajadores/{id:int}/acceso")]
    public async Task<IActionResult> AsignarAcceso(int id, AsignarAccesoRequest request, CancellationToken ct)
    {
        if (request.Estado is not ("Activo" or "Inactivo")) return BadRequest(new { mensaje = "Estado inválido." });
        if (!await db.RolesTrabajadores.AnyAsync(r => r.IdRol == request.IdRol, ct)) return BadRequest(new { mensaje = "Selecciona un rol existente." });
        var t = await db.Trabajadores.FindAsync([id], ct);
        if (t is null) return NotFound();
        t.IdRol = request.IdRol; t.Estado = request.Estado;
        await db.SaveChangesAsync(ct); return NoContent();
    }
}
public sealed record RolRequest
{
    [Required, StringLength(60)] public required string Nombre { get; init; }
    [Required] public required string[] Permisos { get; init; }
}
public sealed record CrearTrabajadorRequest
{
    [Required, StringLength(100)] public required string Nombre { get; init; }
    [Required, EmailAddress, StringLength(100)] public required string Correo { get; init; }
    [Required, StringLength(255, MinimumLength = 12)] public required string Contrasena { get; init; }
    [Range(1, int.MaxValue)] public int IdRol { get; init; }
}
public sealed record AsignarAccesoRequest
{
    [Range(1, int.MaxValue)] public int IdRol { get; init; }
    [Required] public required string Estado { get; init; }
}
