using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.Resenas.Models;

public sealed class Resena
{
    public int IdResena { get; set; }
    public int IdCliente { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;
    public int Calificacion { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
    public DateTime? FechaActualizacionUtc { get; set; }
}
