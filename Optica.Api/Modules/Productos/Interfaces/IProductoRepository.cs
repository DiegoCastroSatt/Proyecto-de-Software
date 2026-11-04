using Optica.Api.Modules.Productos.DTOs;
using Optica.Api.Modules.Productos.Models;

public interface IProductoRepository
{
    Task<Producto> CrearConHistorialStock(Producto producto, int idResponsable);
    Task<bool> ExisteCodigo(string codigo);
    Task<List<Producto>> Buscar(string termino);
    Task<Producto?> ObtenerPorId(int id);
    Task<Producto> ActualizarConHistorialStock(Producto producto, int idResponsable, string motivoMovimiento);
    Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(int idProducto, int pagina, int tamanoPagina);
    Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(int pagina, int tamanoPagina);
    Task<bool> Eliminar(int id);
}