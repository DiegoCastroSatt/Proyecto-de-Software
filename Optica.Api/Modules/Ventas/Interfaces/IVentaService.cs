using Optica.Api.Modules.Ventas.DTOs;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface IVentaService
{
    Task<IReadOnlyList<VentaResponseDto>> Listar();
    Task<VentaCreadaResponseDto> Crear(CrearVentaDto dto);
}
