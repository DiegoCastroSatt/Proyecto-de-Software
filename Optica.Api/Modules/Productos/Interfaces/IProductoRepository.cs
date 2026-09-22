using Optica.Api.Modules.Productos.Models;

public interface IProductoRepository
{
    Task<Producto> Crear(Producto producto);
    Task<bool> ExisteCodigo(string codigo);
    Task<List<Producto>> Buscar(string termino);
    Task<Producto?> ObtenerPorId(int id);
    Task<Producto> Actualizar(Producto producto);
}