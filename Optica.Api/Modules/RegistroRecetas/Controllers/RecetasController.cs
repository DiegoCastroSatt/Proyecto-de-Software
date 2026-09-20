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