using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Marcos;

namespace Optica.Api.Modules.Rostros;

[ApiController]
[Route("api/Rostros")]
public sealed class RostrosController(OpticaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await Consultar(false, ct));

    [AllowAnonymous]
    [HttpGet("activos")]
    public async Task<IActionResult> Activos(CancellationToken ct) => Ok(await Consultar(true, ct));

    private async Task<List<TipoRostroResponse>> Consultar(bool activos, CancellationToken ct)
    {
        var rostros = await db.TiposRostro.AsNoTracking().Where(r => !activos || r.Activo)
            .OrderBy(r => r.Nombre).Select(r => new { r.Id, r.Codigo, r.Nombre, r.Rasgos, r.Consejo, r.Activo, r.RutaImagen }).ToListAsync(ct);
        var marcos = await db.TiposMarco.AsNoTracking().Where(m => !activos || m.Activo).Select(m => new { m.Id, m.Rostros }).ToListAsync(ct);
        return rostros.Select(r => new TipoRostroResponse(r.Id, r.Codigo, r.Nombre, r.Rasgos, r.Consejo, r.Activo, r.RutaImagen,
            marcos.Where(m => m.Rostros.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(r.Codigo)).Select(m => m.Id).ToArray())).ToList();
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/imagen")]
    public async Task<IActionResult> Imagen(int id, CancellationToken ct)
    {
        var imagen = await db.TiposRostro.AsNoTracking().Where(r => r.Id == id)
            .Select(r => new { r.Imagen, r.TipoImagen }).FirstOrDefaultAsync(ct);
        if (imagen?.Imagen is null || imagen.TipoImagen is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(imagen.Imagen, imagen.TipoImagen);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3_300_000)]
    public async Task<IActionResult> Crear([FromForm] CrearTipoRostroRequest request, CancellationToken ct)
    {
        try
        {
            var nombre = request.Nombre.Trim(); var rasgos = request.Rasgos.Trim(); var consejo = request.Consejo.Trim();
            if (nombre.Length is < 1 or > 80 || rasgos.Length is < 1 or > 500 || consejo.Length is < 1 or > 500)
                throw new ArgumentException("Completa el nombre (hasta 80 caracteres), los rasgos y el consejo (hasta 500 cada uno).");
            var nombres = await db.TiposRostro.Select(r => r.Nombre).ToListAsync(ct);
            if (nombres.Any(n => ValidacionMarco.Normalizar(n) == ValidacionMarco.Normalizar(nombre)))
                return Conflict(new { mensaje = "Ya existe un rostro con ese nombre. Puedes reactivarlo si está inactivo." });
            var marcos = await db.TiposMarco.ToListAsync(ct);
            var seleccionados = request.Marcos.Distinct().ToArray();
            if (seleccionados.Any(id => !marcos.Any(m => m.Id == id)))
                throw new ArgumentException("Uno de los marcos seleccionados ya no existe. Actualiza la lista.");
            var (imagen, tipo) = await LeerImagen(request.Imagen, ct);
            var rostro = new TipoRostro { Codigo = Guid.NewGuid().ToString("N"), Nombre = nombre, Rasgos = rasgos, Consejo = consejo, Imagen = imagen, TipoImagen = tipo };
            db.TiposRostro.Add(rostro);
            foreach (var marco in marcos.Where(m => seleccionados.Contains(m.Id)))
                marco.Rostros = string.Join(',', marco.Rostros.Split(',', StringSplitOptions.RemoveEmptyEntries).Append(rostro.Codigo).Distinct());
            await db.SaveChangesAsync(ct);
            return Created($"/api/Rostros/{rostro.Id}/imagen", new TipoRostroResponse(rostro.Id, rostro.Codigo, nombre, rasgos, consejo, true, null, seleccionados));
        }
        catch (ArgumentException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    internal static async Task<(byte[] Imagen, string Tipo)> LeerImagen(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length is <= 0 or > 3_000_000)
            throw new ArgumentException("Selecciona una imagen PNG, JPG o WebP de hasta 3 MB.");
        await using var stream = archivo.OpenReadStream();
        using var memoria = new MemoryStream();
        // Limitar la lectura aunque el tamaño anunciado por el archivo sea incorrecto.
        var buffer = new byte[8192]; int leidos;
        while ((leidos = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (memoria.Length + leidos > 3_000_000) throw new ArgumentException("La imagen supera los 3 MB.");
            memoria.Write(buffer, 0, leidos);
        }
        var bytes = memoria.ToArray();
        var tipo = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? "image/jpeg"
            : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? "image/webp" : null;
        if (tipo is null) throw new ArgumentException("El archivo no corresponde a una imagen PNG, JPG o WebP.");
        return (bytes, tipo);
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, EstadoTipoMarcoRequest request, CancellationToken ct)
    {
        var rostro = await db.TiposRostro.FindAsync([id], ct);
        if (rostro is null) return NotFound(new { mensaje = "No se encuentra el tipo de rostro." });
        rostro.Activo = request.Activo; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("{id:int}/marcos")]
    public async Task<IActionResult> AsociarMarcos(int id, AsociarMarcosRequest request, CancellationToken ct)
    {
        var rostro = await db.TiposRostro.FindAsync([id], ct);
        if (rostro is null) return NotFound(new { mensaje = "No se encuentra el tipo de rostro." });
        var marcos = await db.TiposMarco.ToListAsync(ct);
        if (request.Marcos is null || request.Marcos.Any(idMarco => !marcos.Any(m => m.Id == idMarco)))
            return BadRequest(new { mensaje = "Uno de los marcos seleccionados ya no existe." });
        foreach (var marco in marcos)
        {
            var codigos = marco.Rostros.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(c => c != rostro.Codigo);
            if (request.Marcos.Contains(marco.Id)) codigos = codigos.Append(rostro.Codigo);
            marco.Rostros = string.Join(',', codigos.Distinct());
        }
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Borrar(int id, CancellationToken ct)
    {
        var rostro = await db.TiposRostro.FindAsync([id], ct);
        if (rostro is null) return NotFound(new { mensaje = "No se encuentra el tipo de rostro." });
        foreach (var marco in await db.TiposMarco.ToListAsync(ct))
            marco.Rostros = string.Join(',', marco.Rostros.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(c => c != rostro.Codigo));
        db.TiposRostro.Remove(rostro); await db.SaveChangesAsync(ct); return NoContent();
    }
}
