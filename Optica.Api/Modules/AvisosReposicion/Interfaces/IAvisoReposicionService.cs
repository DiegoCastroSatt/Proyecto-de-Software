using Optica.Api.Modules.AvisosReposicion.DTOs;

namespace Optica.Api.Modules.AvisosReposicion.Interfaces;

public interface IAvisoReposicionService
{
    Task<AvisoReposicionResponseDto> Crear(int idCliente, int idProducto, CancellationToken cancellationToken);
    Task<IReadOnlyList<AvisoReposicionResponseDto>> ListarPorCliente(int idCliente, CancellationToken cancellationToken);
    Task ProcesarReposicionesDisponibles(CancellationToken cancellationToken);
}
