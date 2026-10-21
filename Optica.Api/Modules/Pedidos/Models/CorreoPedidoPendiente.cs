namespace Optica.Api.Modules.Pedidos.Models;

public sealed class CorreoPedidoPendiente
{
    public long Id { get; set; }
    public int PedidoId { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string CuerpoHtml { get; set; } = string.Empty;
    public bool Importante { get; set; }
    public bool Enviado { get; set; }
    public int Intentos { get; set; }
    public DateTime CreadoUtc { get; set; }
    public DateTime ProximoIntentoUtc { get; set; }
    public DateTime? ReclamadoHastaUtc { get; set; }
    public ICollection<IntentoCorreoPedido> Historial { get; set; } = new List<IntentoCorreoPedido>();
}

public sealed class IntentoCorreoPedido
{
    public long Id { get; set; }
    public long CorreoPedidoId { get; set; }
    public DateTime FechaUtc { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
