using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    [HttpGet]
    public async Task<IActionResult> BuscarProductos([FromQuery] string termino = "") => Ok(await _productoService.BuscarProductos(termino));

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
        try
        {
            return Ok(await _productoService.ActualizarProducto(id, dto));
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

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CrearProducto([FromForm] CrearProductoDto dto)
    {
        try
        {
            var producto = await _productoService.CrearProducto(dto);
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
        try
        {
            var producto = await _productoService.CrearProducto(dto);
            return Ok(producto);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}