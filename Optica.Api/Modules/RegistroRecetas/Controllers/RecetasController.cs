using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RecetasController : ControllerBase
{
    private readonly IRecetaService _recetaService;

    public RecetasController(IRecetaService recetaService)
    {
        _recetaService = recetaService;
    }

    [HttpGet("clientes/sugerencias")]
    public async Task<IActionResult> BuscarSugerenciasRut([FromQuery] string termino)
    {
        var sugerencias = await _recetaService.BuscarSugerenciasRut(termino);
        return Ok(sugerencias);
    }

    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorialPorRut([FromQuery] string rut)
    {
        try
        {
            var historial = await _recetaService.ObtenerHistorialPorRut(rut);
            return Ok(historial);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerReceta(int id)
    {
        try { return Ok(await _recetaService.ObtenerReceta(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarReceta(int id, [FromForm] CrearRecetaDto dto)
    {
        try { return Ok(await _recetaService.ActualizarReceta(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> CrearReceta([FromForm] CrearRecetaDto dto)
    {
        try
        {
            var receta = await _recetaService.CrearReceta(dto);
            return Ok(receta);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}