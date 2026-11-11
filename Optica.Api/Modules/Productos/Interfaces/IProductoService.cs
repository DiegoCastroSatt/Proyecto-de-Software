using Optica.Api.Modules.Productos.DTOs;

public interface IProductoService
{
    Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto, int idResponsable);
    Task<List<ProductoResponseDto>> BuscarProductos(string termino);
    Task<ReporteInventarioResponseDto> ConsultarReporteInventario(CancellationToken cancellationToken);
    Task<ProductoResponseDto?> ObtenerProducto(int id);
    Task<ProductoResponseDto> ActualizarProducto(int id, ActualizarProductoDto dto, int idResponsable);
    Task<ProductoResponseDto> ActualizarStock(int id, ActualizarStockDto dto, int idResponsable);
    Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(int idProducto, int pagina, int tamanoPagina);
    Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(int pagina, int tamanoPagina);
    Task EliminarProducto(int id);
}