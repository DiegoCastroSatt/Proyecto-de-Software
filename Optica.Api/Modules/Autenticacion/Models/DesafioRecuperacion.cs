namespace Optica.Api.Modules.Autenticacion.Models;

public sealed class DesafioRecuperacion
{
    public long Id { get; set; }
    public string TipoCuenta { get; set; } = string.Empty;
    public int? IdCuentaCliente { get; set; }
    public int? IdPersonal { get; set; }
    public string HashCodigo { get; set; } = string.Empty;
    public DateTime CreadoUtc { get; set; }
    public DateTime ExpiraUtc { get; set; }
    public int Intentos { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string HuellaIdentificador { get; set; } = string.Empty;
    public string HuellaIp { get; set; } = string.Empty;
    public DateTime? UltimaSolicitudUtc { get; set; }
}
