using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Ventas.DTOs;

public class CrearVentaDto
{
    [Required]
    public string CodigoProducto { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Cantidad { get; set; } = 1;
}
