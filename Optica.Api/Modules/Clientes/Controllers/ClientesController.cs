using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Clientes.Models;

namespace Optica.Api.Modules.Clientes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly OpticaDbContext _context;

    public ClientesController(OpticaDbContext context)
    {
        _context = context;
    }
    
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] Cliente nuevoCliente)
    {
        if (string.IsNullOrWhiteSpace(nuevoCliente.Telefono) && string.IsNullOrWhiteSpace(nuevoCliente.Correo))
        {
            return BadRequest(new { mensaje = "Debe ingresar al menos un medio de contacto (teléfono o correo)." });
        }

        string rutLimpio = nuevoCliente.Rut.Replace(".", "").Trim().ToUpper();

        if (await _context.Clientes.AnyAsync(c => c.Rut == rutLimpio))
        {
            return Conflict(new { mensaje = "Ya existe un cliente registrado con este RUT." });
        }

        nuevoCliente.Rut = rutLimpio;
        nuevoCliente.Estado = "Activo";
        nuevoCliente.FechaRegistro = DateTime.Now;

        _context.Clientes.Add(nuevoCliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Buscar), new { termino = nuevoCliente.Rut }, nuevoCliente);
    }

    // GET: api/clientes/buscar?termino=...
    [HttpGet("buscar")]
    public async Task<ActionResult<IEnumerable<Cliente>>> Buscar([FromQuery] string? termino)
    {
        var query = _context.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            string t = termino.Trim().ToLower();
            query = query.Where(c =>
                c.Rut.ToLower().Contains(t) ||
                c.Nombre.ToLower().Contains(t) ||
                c.Apellido.ToLower().Contains(t)
            );
        }

        var lista = await query
            .OrderBy(c => c.Apellido)
            .ThenBy(c => c.Nombre)
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] Cliente clienteDto)
    {
        // 1. Validar medio de contacto
        if (string.IsNullOrWhiteSpace(clienteDto.Telefono) && string.IsNullOrWhiteSpace(clienteDto.Correo))
        {
            return BadRequest(new { mensaje = "Debe registrar al menos un número de teléfono o correo electrónico." });
        }

        // 2. Buscar el cliente existente por ID
        var clienteExistente = await _context.Clientes.FindAsync(id);
        if (clienteExistente == null)
        {
            return NotFound(new { mensaje = "El cliente no fue encontrado en la base de datos." });
        }

        // 3. Actualizar únicamente los campos editables (el RUT permanece inmutable)
        clienteExistente.Nombre = clienteDto.Nombre.Trim();
        clienteExistente.Apellido = clienteDto.Apellido.Trim();
        clienteExistente.Telefono = string.IsNullOrWhiteSpace(clienteDto.Telefono) ? null : clienteDto.Telefono.Trim();
        clienteExistente.Correo = string.IsNullOrWhiteSpace(clienteDto.Correo) ? null : clienteDto.Correo.Trim();

        try
        {
            await _context.SaveChangesAsync();
            return Ok(clienteExistente);
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new { mensaje = "Error al actualizar los datos en la base de datos.", detalle = ex.Message });
        }
    }
}