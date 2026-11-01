using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Models;

namespace Optica.Api.Modules.Resenas.Interfaces;

public interface IResenaRepository
{
    Task<IReadOnlyList<ResenaResponseDto>> ListarPorProducto(int idProducto, CancellationToken cancellationToken);
    Task<ResumenResenasDto> ObtenerResumen(int idProducto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductoResenableDto>> ListarProductosComprados(int idCliente, CancellationToken cancellationToken);
    Task<bool> ExisteProducto(int idProducto, CancellationToken cancellationToken);
    Task<bool> ClienteComproProducto(int idCliente, int idProducto, CancellationToken cancellationToken);
    Task<bool> YaExisteResena(int idCliente, int idProducto, CancellationToken cancellationToken);
    Task Agregar(Resena resena, CancellationToken cancellationToken);
}
