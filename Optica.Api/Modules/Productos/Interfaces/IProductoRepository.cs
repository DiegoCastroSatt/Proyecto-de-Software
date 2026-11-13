using Optica.Api.Modules.Productos.DTOs;
using Optica.Api.Modules.Productos.Models;

public interface IProductoRepository
{
    Task<Producto> CrearConHistorialStock(Producto producto, int idResponsable);
    Task<bool> ExisteCodigo(string codigo);
    Task<List<Producto>> Buscar(string termino);
    Task<(List<Producto> Productos, int Total)> BuscarPagina(string termino, string estado, string categoria, string color,
        string marca, string ordenPrecio, int desplazamiento, int limite, int? marco = null) => throw new NotSupportedException();
    Task<(string[] Categorias, string[] Colores, string[] Marcas)> OpcionesBusqueda(string termino) =>
        throw new NotSupportedException();
    Task<List<Producto>> ObtenerParaReporteInventario(CancellationToken cancellationToken);
    Task<Producto?> ObtenerPorId(int id);
    Task<Producto> ActualizarConHistorialStock(Producto producto, int idResponsable, string motivoMovimiento);
    Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(int idProducto, int pagina, int tamanoPagina);
    Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(int pagina, int tamanoPagina);
    Task<bool> Eliminar(int id);
}