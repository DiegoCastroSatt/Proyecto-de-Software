using Optica.Api.Modules.Ventas.DTOs;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface IConsultaProductoVenta
{
    Task<ProductoCajaDto?> Buscar(string codigo);
}
