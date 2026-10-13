using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.RegistroRecetas.Models;

public interface IRecetaRepository
{
    Task<Cliente?> BuscarClientePorRut(string rut);
    Task<Receta> Crear(Receta receta);
    Task<Receta> Actualizar(Receta receta);
    Task<Cliente?> ObtenerClientePorId(int id);
    Task<Receta?> ObtenerPorId(int id);
    Task<List<Cliente>> BuscarClientesPorRutParcial(string rutParcial);
    Task<List<Receta>> ObtenerRecetasPorClienteId(int clienteId);
}