using Optica.Api.Modules.AvisosReposicion.DTOs;
using Optica.Api.Modules.AvisosReposicion.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.AvisosReposicion.Interfaces;

public interface IAvisoReposicionRepository
{
    Task<Cliente?> ObtenerCliente(int idCliente, CancellationToken cancellationToken);
    Task<Producto?> ObtenerProducto(int idProducto, CancellationToken cancellationToken);
    Task<bool> ExisteAvisoAbierto(string rut, int idProducto, CancellationToken cancellationToken);
    Task<AvisoReposicion> Crear(AvisoReposicion aviso, CancellationToken cancellationToken);
    Task<IReadOnlyList<AvisoReposicionResponseDto>> ListarPorCliente(int idCliente, CancellationToken cancellationToken);
    Task<IReadOnlyList<AvisoReposicionAdministracionResponseDto>> ListarTodas(CancellationToken cancellationToken);
    Task<IReadOnlyList<AvisoReposicionAdministracionResponseDto>> ListarPendientesConStock(CancellationToken cancellationToken);
    Task Cerrar(int idAviso, CancellationToken cancellationToken);
}
