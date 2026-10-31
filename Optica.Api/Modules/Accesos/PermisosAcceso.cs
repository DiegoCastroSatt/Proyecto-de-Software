using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace Optica.Api.Modules.Accesos;

public static class PermisosAcceso
{
    public const string Tipo = "tipo_usuario";
    public const string Permiso = "permiso";
    public const string Administrador = "Administrador";
    public const string Trabajador = "Trabajador";
    public const string Personal = "Personal";
    public const string SoloAdministrador = "SoloAdministrador";
    public const string SoloTrabajador = "CuentaTrabajador";
    public const string VersionCredencial = "version_credencial";
    public const string Responsable = "administrador_responsable";
    public static readonly IReadOnlyDictionary<string, string> Catalogo = new Dictionary<string, string>
    {
        ["resumen"] = "Ver resumen general de clientes, stock, pedidos y ventas",
        ["horarios"] = "Gestionar horarios de atención",
        ["agenda"] = "Consultar, reprogramar y cancelar reservas",
        ["clientes"] = "Gestionar clientes y consultar su historial",
        ["inventario"] = "Gestionar productos, catálogos y stock",
        ["ventas"] = "Registrar y consultar ventas",
        ["recetas"] = "Registrar, editar y consultar recetas y graduaciones",
        ["pedidos"] = "Crear, consultar y actualizar pedidos"
    };
    public static bool Tiene(ClaimsPrincipal usuario, string permiso) =>
        usuario.Identity?.IsAuthenticated == true &&
        (usuario.HasClaim(Tipo, Administrador) ||
        (usuario.HasClaim(Tipo, Trabajador) && usuario.HasClaim(Permiso, permiso)));
    public static void Configurar(AuthorizationOptions options)
    {
        options.AddPolicy(SoloAdministrador, p => p.RequireAuthenticatedUser().RequireClaim(Tipo, Administrador));
        options.AddPolicy(SoloTrabajador, p => p.RequireAuthenticatedUser().RequireClaim(Tipo, Trabajador));
        options.AddPolicy(Personal, p => p.RequireAuthenticatedUser().RequireClaim(Tipo, Administrador, Trabajador));
        foreach (var permiso in Catalogo.Keys)
            options.AddPolicy(permiso, p => p.RequireAuthenticatedUser().RequireAssertion(c => Tiene(c.User, permiso)));
        // Reservar al administrador los endpoints internos nuevos sin política explícita.
        options.DefaultPolicy = options.GetPolicy(SoloAdministrador)!;
        options.FallbackPolicy = options.DefaultPolicy;
    }
}
