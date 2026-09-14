using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;

namespace Optica.Api.Modules.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController(IVentaService ventas, IConsultaProductoVenta productos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await ventas.Listar());

    [HttpGet("producto")]
    public async Task<IActionResult> BuscarProducto([FromQuery] string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new ErrorVentaDto("Ingresa un código."));
        var producto = await productos.Buscar(codigo);
        return producto is null
            ? NotFound(new ErrorVentaDto("No se encontró un producto con ese código."))
            : Ok(producto);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearVentaDto dto)
    {
        try
        {
            return CreatedAtAction(nameof(Listar), await ventas.Crear(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorVentaDto(ex.Message));
        }
    }
}
