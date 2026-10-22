using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;

namespace Optica.Api.Modules.Marcos;

[ApiController]
[Route("api/Marcos")]
public sealed class MarcosController(OpticaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TipoMarcoResponse>>> Listar(CancellationToken ct) => Ok(await Consultar(false, ct));

    [AllowAnonymous]
    [HttpGet("disponibles")]
    public async Task<ActionResult<List<TipoMarcoResponse>>> Disponibles(CancellationToken ct) => Ok(await Consultar(true, ct));

    private async Task<List<TipoMarcoResponse>> Consultar(bool disponibles, CancellationToken ct)
    {
        var tipos = await db.TiposMarco.AsNoTracking().Where(t => !disponibles || t.Activo).OrderBy(t => t.Nombre).ToListAsync(ct);
        var productos = await db.Productos.AsNoTracking().Where(p => p.IdTipoMarco != null)
            .Select(p => new { p.IdTipoMarco, p.Stock, p.Estado, p.Categoria }).ToListAsync(ct);
        productos = productos.Where(p => ValidacionMarco.EsArmazon(p.Categoria)).ToList();
        var stocks = productos.Where(p => p.Stock > 0 && p.Estado == "Disponible")
            .GroupBy(p => p.IdTipoMarco!.Value).ToDictionary(g => g.Key, g => g.Sum(p => p.Stock));
        var vinculados = productos.GroupBy(p => p.IdTipoMarco!.Value).ToDictionary(g => g.Key, g => g.Count());
        var rostrosActivos = await db.TiposRostro.Where(r => r.Activo).Select(r => r.Codigo).ToListAsync(ct);
        return tipos.Select(t => new TipoMarcoResponse(t.Id, t.Codigo, t.Nombre, t.Categoria, t.Activo,
            t.Rostros.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(r => !disponibles || rostrosActivos.Contains(r)).ToArray(), stocks.GetValueOrDefault(t.Id), vinculados.GetValueOrDefault(t.Id)))
            .Where(t => !disponibles || t.Stock > 0).ToList();
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/imagen")]
    public async Task<IActionResult> Imagen(int id, CancellationToken ct)
    {
        var svg = await db.TiposMarco.Where(t => t.Id == id).Select(t => t.Svg).FirstOrDefaultAsync(ct);
        if (svg is null) return NotFound();
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'none'; sandbox";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Content(svg, "image/svg+xml; charset=utf-8");
    }

    [HttpPost("validar-diseno")]
    [RequestSizeLimit(150_000)]
    public IActionResult ValidarDiseno([FromBody] ValidarDisenoMarcoRequest request)
    {
        try { return Ok(new { svg = ValidacionMarco.ValidarSvg(request.Svg) }); }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPost]
    [RequestSizeLimit(150_000)]
    public async Task<IActionResult> Crear(CrearTipoMarcoRequest request, CancellationToken ct)
    {
        try
        {
            var nombre = request.Nombre?.Trim() ?? "";
            var categoria = string.IsNullOrWhiteSpace(request.Categoria) ? "Armazones" : request.Categoria.Trim();
            if (nombre.Length is < 1 or > 80 || categoria.Length is < 1 or > 50)
                throw new ArgumentException("Indica un nombre de hasta 80 caracteres y una categoría de hasta 50.");
            var rostros = request.Rostros?.Distinct().ToArray() ?? [];
            var rostrosActivos = await db.TiposRostro.Where(r => r.Activo).Select(r => r.Codigo).ToListAsync(ct);
            if (rostros.Length == 0 || rostros.Any(r => !rostrosActivos.Contains(r)))
                throw new ArgumentException("Selecciona al menos un rostro recomendado válido.");
            var svg = ValidacionMarco.ValidarSvg(request.Svg);
            var tipos = await db.TiposMarco.AsNoTracking().Select(t => new { t.Nombre }).ToListAsync(ct);
            if (tipos.Any(t => ValidacionMarco.Normalizar(t.Nombre) == ValidacionMarco.Normalizar(nombre)))
                return Conflict(new { mensaje = "Ya existe un tipo de marco con ese nombre. Puedes reactivar el existente." });
            var tipo = new TipoMarco { Codigo = Guid.NewGuid().ToString("N"), Nombre = nombre, Categoria = categoria, Svg = svg, Rostros = string.Join(',', rostros) };
            db.TiposMarco.Add(tipo);
            // Incluir la categoría en el selector de productos para poder asignarle stock.
            if (!await db.Catalogos.AnyAsync(c => c.Tipo == "Categoria" && c.Nombre == categoria, ct))
                db.Catalogos.Add(new() { Tipo = "Categoria", Nombre = categoria });
            await db.SaveChangesAsync(ct);
            var respuesta = (await Consultar(false, ct)).Single(t => t.Id == tipo.Id);
            return Created($"/api/Marcos/{tipo.Id}/imagen", respuesta);
        }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoTipoMarcoRequest request, CancellationToken ct)
    {
        var tipo = await db.TiposMarco.FindAsync([id], ct);
        if (tipo is null) return NotFound(new { mensaje = "No se encuentra el tipo de marco." });
        tipo.Activo = request.Activo;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("productos-vinculables")]
    public async Task<IActionResult> ProductosVinculables(CancellationToken ct)
    {
        var productos = await db.Productos.AsNoTracking().OrderBy(p => p.Nombre)
            .Select(p => new ProductoVinculableMarcoResponse(p.IdProducto, p.Codigo, p.Nombre, p.Categoria, p.Marca, p.Stock, p.Estado, p.IdTipoMarco)).ToListAsync(ct);
        return Ok(productos.Where(p => ValidacionMarco.EsArmazon(p.Categoria)).ToList());
    }

    [HttpPut("{id:int}/productos")]
    public async Task<IActionResult> VincularProductos(int id, VincularProductosMarcoRequest request, CancellationToken ct)
    {
        if (!await db.TiposMarco.AnyAsync(t => t.Id == id, ct))
            return NotFound(new { mensaje = "No se encuentra el tipo de marco." });
        if (request.Productos is null) return BadRequest(new { mensaje = "Indica los productos que quieres vincular." });
        var seleccion = request.Productos.Distinct().ToArray();
        var productos = await db.Productos.Where(p => p.IdTipoMarco == id || seleccion.Contains(p.IdProducto)).ToListAsync(ct);
        if (seleccion.Any(idProducto => !productos.Any(p => p.IdProducto == idProducto)))
            return BadRequest(new { mensaje = "Uno de los productos ya no existe. Actualiza la lista antes de guardar." });
        if (productos.Any(p => seleccion.Contains(p.IdProducto) && !ValidacionMarco.EsArmazon(p.Categoria)))
            return BadRequest(new { mensaje = "Solo puedes vincular productos de la categoría Armazones." });
        foreach (var producto in productos)
            producto.IdTipoMarco = seleccion.Contains(producto.IdProducto) ? id : null;
        await db.SaveChangesAsync(ct);
        return Ok(await Consultar(false, ct));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var tipo = await db.TiposMarco.FindAsync([id], ct);
        if (tipo is null) return NotFound(new { mensaje = "No se encuentra el tipo de marco." });
        foreach (var producto in await db.Productos.Where(p => p.IdTipoMarco == id).ToListAsync(ct))
            producto.IdTipoMarco = null;
        db.TiposMarco.Remove(tipo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
