using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface IVentaRepository
{
    Task<IReadOnlyList<VentaResponseDto>> Listar();
    Task Guardar(Venta venta);
}
