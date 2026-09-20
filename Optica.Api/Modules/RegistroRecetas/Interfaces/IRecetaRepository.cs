using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.RegistroRecetas.Models;

public interface IRecetaRepository
{
    Task<Cliente?> BuscarClientePorRut(string rut);
    Task<Receta> Crear(Receta receta);
    Task<Receta?> ObtenerPorId(int id);
}