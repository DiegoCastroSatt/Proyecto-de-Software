namespace Optica.Api.Modules.Clientes.Models;

public sealed class CuentaCliente
{
    public int IdCuenta { get; set; }
    public int IdCliente { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string CorreoNormalizado { get; set; } = string.Empty;
    public string ContrasenaHash { get; set; } = string.Empty;
    public string Estado { get; set; } = "Activa";
    public DateTime FechaCreacion { get; set; }
    public string? TokenConfirmacionHash { get; set; }
    public DateTime? TokenConfirmacionExpiraUtc { get; set; }
    public DateTime? UltimoCorreoConfirmacionUtc { get; set; }
    public Cliente? Cliente { get; set; }
}
