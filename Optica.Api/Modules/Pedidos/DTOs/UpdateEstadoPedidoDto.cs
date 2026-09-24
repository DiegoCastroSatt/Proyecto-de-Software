using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Pedidos.DTOs;

public class UpdateEstadoPedidoDto
{
    [Required]
    public string Estado { get; set; } = string.Empty;
}
