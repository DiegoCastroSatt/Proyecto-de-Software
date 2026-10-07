using Optica.Api.Modules.Clientes.DTOs;
using Optica.Api.Modules.Clientes.Interfaces;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;

    public ClienteService(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Cliente>> BuscarAsync(string? termino)
    {
        return await _repository.BuscarAsync(termino);
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        return await _repository.ObtenerPorIdAsync(id);
    }

    public async Task<Cliente> RegistrarAsync(CrearClienteDto dto)
    {
        // Regla 1: Validar medio de contacto
        if (string.IsNullOrWhiteSpace(dto.Telefono) && string.IsNullOrWhiteSpace(dto.Correo))
        {
            throw new ArgumentException("Debe ingresar al menos un medio de contacto (teléfono o correo).");
        }

        // Regla 2: Formatear RUT
        string rutLimpio = dto.Rut.Replace(".", "").Trim().ToUpper();

        // Regla 3: Validar unicidad de RUT
        if (await _repository.ExisteRutAsync(rutLimpio))
        {
            throw new InvalidOperationException("Ya existe un cliente registrado con este RUT.");
        }

        var nuevoCliente = new Cliente
        {
            Rut = rutLimpio,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim(),
            Estado = "Activo",
            FechaRegistro = DateTime.Now
        };

        return await _repository.CrearAsync(nuevoCliente);
    }

    public async Task<Cliente> ActualizarAsync(int id, ActualizarClienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Telefono) && string.IsNullOrWhiteSpace(dto.Correo))
        {
            throw new ArgumentException("Debe registrar al menos un número de teléfono o correo electrónico.");
        }

        var cliente = await _repository.ObtenerPorIdAsync(id);
        if (cliente == null)
        {
            throw new KeyNotFoundException("El cliente no fue encontrado en la base de datos.");
        }

        cliente.Nombre = dto.Nombre.Trim();
        cliente.Apellido = dto.Apellido.Trim();
        cliente.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        cliente.Correo = string.IsNullOrWhiteSpace(dto.Correo) ? null : dto.Correo.Trim();

        await _repository.ActualizarAsync(cliente);
        return cliente;
    }

    public async Task<Cliente> CambiarEstadoAsync(int id, string nuevoEstado)
    {
        if (string.IsNullOrWhiteSpace(nuevoEstado) || 
            (nuevoEstado != "Activo" && nuevoEstado != "Inactivo"))
        {
            throw new ArgumentException("El estado debe ser 'Activo' o 'Inactivo'.");
        }

        var cliente = await _repository.ObtenerPorIdAsync(id);
        if (cliente == null)
        {
            throw new KeyNotFoundException("El cliente no fue encontrado en la base de datos.");
        }

        cliente.Estado = nuevoEstado;
        await _repository.ActualizarAsync(cliente);
        return cliente;
    }
    public async Task<HistorialClienteDto?> ObtenerHistorialAsync(int idCliente)
{
    var historial = await _repository.ObtenerHistorialAsync(idCliente);
    if (historial == null)
    {
        throw new KeyNotFoundException($"No se encontró el cliente con ID {idCliente}.");
    }
    return historial;
}
}