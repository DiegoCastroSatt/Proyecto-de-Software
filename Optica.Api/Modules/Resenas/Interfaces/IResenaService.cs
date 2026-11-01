using Optica.Api.Modules.Resenas.DTOs;

namespace Optica.Api.Modules.Resenas.Interfaces;

public interface IResenaService
{
    Task<IReadOnlyList<ResenaResponseDto>> ListarPorProducto(int idProducto, CancellationToken cancellationToken);
    Task<ResumenResenasDto> ObtenerResumen(int idProducto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductoResenableDto>> ListarProductosComprados(int idCliente, CancellationToken cancellationToken);
    Task<ResenaResponseDto> Crear(int idCliente, int idProducto, CrearResenaDto dto, CancellationToken cancellationToken);
}
