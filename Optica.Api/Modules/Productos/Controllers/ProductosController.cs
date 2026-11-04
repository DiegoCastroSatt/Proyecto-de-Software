using System.Security.Claims;
using Optica.Api.Modules.Accesos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "inventario")]
[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> BuscarProductos([FromQuery] string termino = "") => Ok(await _productoService.BuscarProductos(termino));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerProducto(int id)
    {
        var producto = await _productoService.ObtenerProducto(id);
        return producto is null ? NotFound(new { mensaje = "No se encuentra el producto." }) : Ok(producto);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ActualizarProducto(int id, [FromForm] ActualizarProductoDto dto)
    {
        if (!TryObtenerIdResponsable(out var idResponsable))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _productoService.ActualizarProducto(id, dto, idResponsable));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> ActualizarStock(int id, [FromBody] ActualizarStockDto dto)
    {
        if (!TryObtenerIdResponsable(out var idResponsable))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _productoService.ActualizarStock(id, dto, idResponsable));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{id:int}/historial-stock")]
    public async Task<IActionResult> ConsultarHistorialStock(
        int id,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        try
        {
            var historial = await _productoService.ConsultarHistorialStock(id, pagina, tamanoPagina);
            return historial is null
                ? NotFound(new { mensaje = "No se encuentra el producto." })
                : Ok(historial);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> EliminarProducto(int id)
    {
        try
        {
            await _productoService.EliminarProducto(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CrearProducto([FromForm] CrearProductoDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idResponsable) || idResponsable <= 0)
        {
            return Unauthorized();
        }

        try
        {
            var producto = await _productoService.CrearProducto(dto, idResponsable);
            return Ok(producto);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPost("json")]
    [Consumes("application/json")]
    public async Task<IActionResult> CrearProductoJson([FromBody] CrearProductoDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idResponsable) || idResponsable <= 0)
        {
            return Unauthorized();
        }

        try
        {
            var producto = await _productoService.CrearProducto(dto, idResponsable);
            return Ok(producto);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    private bool TryObtenerIdResponsable(out int idResponsable) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out idResponsable) && idResponsable > 0;
}