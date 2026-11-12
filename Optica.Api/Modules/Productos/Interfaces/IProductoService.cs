using Optica.Api.Modules.Productos.DTOs;

public interface IProductoService
{
    Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto, int idResponsable);
    Task<List<ProductoResponseDto>> BuscarProductos(string termino);
    Task<(List<ProductoResponseDto> Productos, int Total)> BuscarPagina(string termino, string estado, string categoria, string color,
        string marca, string ordenPrecio, int desplazamiento, int limite) => throw new NotSupportedException();
    Task<(string[] Categorias, string[] Colores, string[] Marcas)> OpcionesBusqueda(string termino) =>
        throw new NotSupportedException();
    Task<ReporteInventarioResponseDto> ConsultarReporteInventario(CancellationToken cancellationToken);
    Task<ProductoResponseDto?> ObtenerProducto(int id);
    Task<ProductoResponseDto> ActualizarProducto(int id, ActualizarProductoDto dto, int idResponsable);
    Task<ProductoResponseDto> ActualizarStock(int id, ActualizarStockDto dto, int idResponsable);
    Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(int idProducto, int pagina, int tamanoPagina);
    Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(int pagina, int tamanoPagina);
    Task EliminarProducto(int id);
}