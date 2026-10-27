using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Ventas.DTOs;

public class CrearVentaDto
{
    [MaxLength(13)]
    public string? RutCliente { get; set; }

    [Required, MinLength(1)]
    public List<ProductoVentaDto> Productos { get; set; } = [];
}

public class ProductoVentaDto
{
    [Required]
    public string CodigoProducto { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;
}
