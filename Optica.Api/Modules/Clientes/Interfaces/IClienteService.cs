using Optica.Api.Modules.Clientes.DTOs;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Interfaces;

public interface IClienteService
{
    Task<IEnumerable<Cliente>> BuscarAsync(string? termino);
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<Cliente> RegistrarAsync(CrearClienteDto dto);
    Task<Cliente> ActualizarAsync(int id, ActualizarClienteDto dto);
    Task<Cliente> CambiarEstadoAsync(int id, string nuevoEstado);
    Task<HistorialClienteDto?> ObtenerHistorialAsync(int idCliente);
}