namespace Optica.Api.Modules.AgendaReservas.Models;

public class Administrador
{
    public int IdAdministrador { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Estado { get; set; } = "Activo";
}
