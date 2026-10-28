using Optica.Api.Modules.Accesos;
namespace Optica.Api.Modules.Autenticacion.DTOs;

/// <summary>Identidad del personal autenticado, sin contraseñas.</summary>
public sealed record LoginAdministradorResponse(int IdAdministrador, string Nombre)
{
    public string TipoUsuario { get; init; } = PermisosAcceso.Administrador;
    public int? IdTrabajador { get; init; }
    public string Rol { get; init; } = "Administrador";
    public string[] Permisos { get; init; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public int IdAdministradorResponsable { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string VersionCredencial { get; init; } = "";
}
