using System;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class PedidoResponseDto
{
    public int IdPedido { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? Anotaciones { get; set; }
    public string? EstadoNotificacion { get; set; }
    public DateTime? FechaEntregaEstimada { get; set; }
    public bool Vencido { get; set; }
}

public record PedidosPaginaResponseDto(IReadOnlyList<PedidoResponseDto> Pedidos, bool HaySiguiente);
