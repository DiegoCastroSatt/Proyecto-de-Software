using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Optica.Api.Modules.Accesos;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;

namespace Optica.Api.Modules.Ventas.Controllers;

[Authorize(Policy = "ventas")]
[ApiController]
[Route("api/[controller]")]
public class VentasController(IVentaService ventas, IConsultaProductoVenta productos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await ventas.Listar());

    [HttpGet("pagina")]
    public async Task<IActionResult> ListarPagina([FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
    {
        if (pagina < 1 || tamano is < 1 or > 100 || (pagina - 1L) * tamano > int.MaxValue)
            return BadRequest(new ErrorVentaDto("La página o su tamaño no son válidos."));
        return Ok(await ventas.ListarPagina((pagina - 1) * tamano, tamano));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumir(DateTime desde, DateTime hasta, DateTime desdeRanking, DateTime hastaRanking, int limite = 5)
    {
        if (hasta <= desde || (hasta - desde).TotalDays > 366 || hastaRanking < desdeRanking
            || (hastaRanking - desdeRanking).TotalDays > 366 || limite is not (5 or 10))
            return BadRequest(new ErrorVentaDto("El período o el límite no son válidos."));
        return Ok(await ventas.Resumir(desde, hasta, desdeRanking, hastaRanking, limite));
    }

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
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idResponsable) || idResponsable <= 0)
            return Unauthorized();

        try
        {
            return CreatedAtAction(nameof(Listar), await ventas.Crear(dto, idResponsable));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorVentaDto(ex.Message));
        }
    }
}
