using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Autenticacion.DTOs;
namespace Optica.Api.Modules.Accesos;

public sealed class AccesoService(OpticaDbContext db, IPasswordHasher<Trabajador> hasher)
{
    public static string[] LeerPermisos(string json) => (JsonSerializer.Deserialize<string[]>(json) ?? [])
        .Where(PermisosAcceso.Catalogo.ContainsKey).Distinct().ToArray();
    public static LoginAdministradorResponse Respuesta(Trabajador t) => new(0, t.Nombre)
    {
        TipoUsuario = PermisosAcceso.Trabajador, IdTrabajador = t.IdTrabajador,
        Rol = t.Rol.Nombre, Permisos = LeerPermisos(t.Rol.PermisosJson),
        IdAdministradorResponsable = t.IdAdministradorCreador ?? t.IdTrabajador,
        VersionCredencial = CrearVersionCredencial(t.Contrasena)
    };
    public static string CrearVersionCredencial(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));
    public static ClaimsPrincipal CrearPrincipal(LoginAdministradorResponse sesion)
    {
        var admin = sesion.TipoUsuario == PermisosAcceso.Administrador;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, (admin ? sesion.IdAdministrador : sesion.IdTrabajador!.Value).ToString()),
            new(ClaimTypes.Name, sesion.Nombre), new(PermisosAcceso.Tipo, sesion.TipoUsuario),
            new(ClaimTypes.Role, admin ? PermisosAcceso.Administrador : sesion.Rol),
            new(PermisosAcceso.Responsable, (admin ? sesion.IdAdministrador : sesion.IdAdministradorResponsable).ToString())
        };
        claims.Add(new Claim(PermisosAcceso.VersionCredencial, sesion.VersionCredencial));
        if (!admin) claims.AddRange(sesion.Permisos.Where(PermisosAcceso.Catalogo.ContainsKey)
            .Select(p => new Claim(PermisosAcceso.Permiso, p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
    public async Task<LoginAdministradorResponse?> IniciarTrabajadorAsync(LoginAdministradorRequest request, CancellationToken ct)
    {
        var usuario = request.NombreUsuario.Trim().ToUpperInvariant();
        if (usuario.Contains('@')) return null;

        var t = await db.Trabajadores.Include(t => t.Rol).SingleOrDefaultAsync(t => !(t is Optica.Api.Modules.AgendaReservas.Models.Administrador) && t.NombreUsuario.ToUpper() == usuario && t.Estado == "Activo", ct);
        if (t is null) return null;
        var resultado = hasher.VerifyHashedPassword(t, t.Contrasena, request.Contrasena);
        if (resultado == PasswordVerificationResult.Failed) return null;
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            t.Contrasena = hasher.HashPassword(t, request.Contrasena);
            await db.SaveChangesAsync(ct);
        }
        return Respuesta(t);
    }
    public async Task<LoginAdministradorResponse?> ObtenerSesionAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        var tipo = principal.FindFirstValue(PermisosAcceso.Tipo);
        if (tipo == PermisosAcceso.Trabajador)
        {
            var t = await db.Trabajadores.AsNoTracking().Include(t => t.Rol)
                .SingleOrDefaultAsync(t => !(t is Optica.Api.Modules.AgendaReservas.Models.Administrador) && t.IdTrabajador == id && t.Estado == "Activo", ct);
            // El cambio de contraseña invalida todas las cookies emitidas con la clave anterior.
            if (t is null || principal.FindFirstValue(PermisosAcceso.VersionCredencial) != CrearVersionCredencial(t.Contrasena)) return null;
            return Respuesta(t);
        }
        // También se revalida la versión de contraseña de las cuentas administrativas.
        if (tipo is not null && tipo != PermisosAcceso.Administrador) return null;
        var admin = await db.Administradores.AsNoTracking().SingleOrDefaultAsync(a => a.IdTrabajador == id && a.Estado == "Activo", ct);
        return admin is null || principal.FindFirstValue(PermisosAcceso.VersionCredencial) != CrearVersionCredencial(admin.Contrasena)
            ? null : new LoginAdministradorResponse(admin.IdAdministrador, admin.Nombre)
            { VersionCredencial = CrearVersionCredencial(admin.Contrasena) };
    }
}
