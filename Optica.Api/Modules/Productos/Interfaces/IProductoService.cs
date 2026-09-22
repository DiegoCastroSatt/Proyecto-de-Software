public interface IProductoService
{
    Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto);
    Task<List<ProductoResponseDto>> BuscarProductos(string termino);
    Task<ProductoResponseDto?> ObtenerProducto(int id);
    Task<ProductoResponseDto> ActualizarProducto(int id, ActualizarProductoDto dto);
    Task EliminarProducto(int id);
}