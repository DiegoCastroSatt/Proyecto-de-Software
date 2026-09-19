using Optica.Api.Modules.Productos.Models;

public interface IProductoRepository
{
    Task<Producto> Crear(Producto producto);
    Task<bool> ExisteCodigo(string codigo);
}