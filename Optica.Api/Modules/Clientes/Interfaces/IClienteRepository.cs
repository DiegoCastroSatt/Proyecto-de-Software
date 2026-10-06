using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Interfaces;

public interface IClienteRepository
{
    Task<IEnumerable<Cliente>> BuscarAsync(string? termino);
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<Cliente?> ObtenerPorRutAsync(string rut);
    Task<bool> ExisteRutAsync(string rut);
    Task<Cliente> CrearAsync(Cliente cliente);
    Task ActualizarAsync(Cliente cliente);
}