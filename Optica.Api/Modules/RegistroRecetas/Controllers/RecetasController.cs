using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Autenticacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecetasController : ControllerBase
{
    private readonly IRecetaService _recetaService;

    public RecetasController(IRecetaService recetaService)
    {
        _recetaService = recetaService;
    }

    [Authorize(Policy = "recetas")]
    [HttpGet("clientes/sugerencias")]
    public async Task<IActionResult> BuscarSugerenciasRut([FromQuery] string termino)
    {
        var sugerencias = await _recetaService.BuscarSugerenciasRut(termino);
        return Ok(sugerencias);
    }

    [Authorize(Policy = "recetas")]
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

    [Authorize(Policy = AutenticacionClientesController.Politica)]
    [HttpGet("mis-recetas")]
    public async Task<IActionResult> ObtenerMisRecetas()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente))
            return Unauthorized();

        var historial = await _recetaService.ObtenerHistorialPorClienteId(idCliente);
        return Ok(historial);
    }

    [Authorize(Policy = "recetas")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerReceta(int id)
    {
        try { return Ok(await _recetaService.ObtenerReceta(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }

    [Authorize(Policy = "recetas")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> ActualizarReceta(int id, [FromForm] CrearRecetaDto dto)
    {
        try { return Ok(await _recetaService.ActualizarReceta(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [Authorize(Policy = "recetas")]
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