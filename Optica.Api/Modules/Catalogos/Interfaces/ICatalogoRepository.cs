using Optica.Api.Modules.Catalogos.Models;

public interface ICatalogoRepository
{
    Task<IReadOnlyList<CatalogoItem>> ObtenerPorTipo(string tipo);
    Task<CatalogoItem?> ObtenerPorNombre(string tipo, string nombre);
    Task<CatalogoItem> Crear(CatalogoItem item);
}
