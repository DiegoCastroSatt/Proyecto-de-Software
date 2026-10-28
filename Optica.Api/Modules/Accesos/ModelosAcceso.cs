namespace Optica.Api.Modules.Accesos;

public sealed class RolTrabajador
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public string PermisosJson { get; set; } = "[]";
}
public sealed class Trabajador
{
    public int IdTrabajador { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public string Estado { get; set; } = "Activo";
    public int IdRol { get; set; }
    public RolTrabajador Rol { get; set; } = null!;
    public int IdAdministradorCreador { get; set; }
}
