using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.AvisosReposicion.Models;

public sealed class AvisoReposicion
{
    public int IdAviso { get; set; }
    public string Rut { get; set; } = string.Empty;
    public int IdProducto { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "Abierta";
    public Producto Producto { get; set; } = null!;
}
