using System.Security.Claims;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Productos.DTOs;
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
    [HttpGet("pagina")]
    public async Task<IActionResult> BuscarPagina([FromQuery] int pagina = 1, [FromQuery] int tamano = 20,
        [FromQuery] string? termino = null, [FromQuery] string? estado = null, [FromQuery] string? categoria = null,
        [FromQuery] string? color = null, [FromQuery] string? marca = null, [FromQuery] string? ordenPrecio = null,
        [FromQuery] bool incluirOpciones = false)
    {
        if (pagina < 1 || tamano is < 1 or > 100 || (pagina - 1L) * tamano > int.MaxValue)
            return BadRequest(new { mensaje = "La página o su tamaño no son válidos." });

        var (productos, total) = await _productoService.BuscarPagina(termino ?? "", estado ?? "", categoria ?? "",
            color ?? "", marca ?? "", ordenPrecio ?? "", (pagina - 1) * tamano, tamano);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamano);
        var haySiguiente = pagina < totalPaginas;
        if (!incluirOpciones) return Ok(new { productos, haySiguiente, totalPaginas });
        var opciones = await _productoService.OpcionesBusqueda(termino ?? "");
        return Ok(new { productos, haySiguiente, totalPaginas, opciones.Categorias, opciones.Colores, opciones.Marcas });
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerProducto(int id)
    {
        var producto = await _productoService.ObtenerProducto(id);
        return producto is null ? NotFound(new { mensaje = "No se encuentra el producto." }) : Ok(producto);
    }

    [Authorize(Policy = PermisosAcceso.SoloAdministrador)]
    [HttpGet("reporte-inventario")]
    [ProducesResponseType(typeof(ReporteInventarioResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteInventarioResponseDto>> ConsultarReporteInventario(
        CancellationToken cancellationToken) =>
        Ok(await _productoService.ConsultarReporteInventario(cancellationToken));

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

    [HttpGet("historial-stock")]
    public async Task<IActionResult> ConsultarHistorialStockReciente(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20)
    {
        try
        {
            return Ok(await _productoService.ConsultarHistorialStockReciente(pagina, tamanoPagina));
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