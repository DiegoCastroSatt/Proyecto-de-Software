public interface IProductoService
{
    Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto);
}