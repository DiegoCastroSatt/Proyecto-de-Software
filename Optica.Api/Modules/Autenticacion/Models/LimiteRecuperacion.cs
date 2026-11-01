namespace Optica.Api.Modules.Autenticacion.Models;

public sealed class LimiteRecuperacion
{
    public string Huella { get; set; } = string.Empty;
    public DateTime InicioVentanaUtc { get; set; }
    public int Solicitudes { get; set; }
    public DateTime UltimaSolicitudUtc { get; set; }
}
