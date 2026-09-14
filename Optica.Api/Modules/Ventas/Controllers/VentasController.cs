using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Models;

namespace Optica.Api.Modules.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController(OpticaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var ventas = await db.Ventas.AsNoTracking()
            .OrderByDescending(v => v.Fecha)
            .Select(v => new
            {
                v.IdVenta,
                v.Fecha,
                v.Total,
                Productos = v.Detalles.Select(d => new
                {
                    d.ProductoId,
                    Nombre = db.Productos.Where(p => p.IdProducto == d.ProductoId)
                        .Select(p => p.Nombre).FirstOrDefault(),
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal
                })
            })
            .ToListAsync();
        return Ok(ventas);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearVentaDto dto)
    {
        var codigo = dto.CodigoProducto.Trim();
        if (codigo.Length == 0)
            return BadRequest(new { mensaje = "El código del producto es obligatorio." });

        var producto = await db.Productos.AsNoTracking().SingleOrDefaultAsync(p => p.Codigo == codigo);
        if (producto is null)
            return BadRequest(new { mensaje = "No se encontró un producto con ese código." });

        var subtotal = producto.Precio * dto.Cantidad;
        var venta = new Venta
        {
            Fecha = DateTime.Now,
            Total = subtotal,
            Detalles =
            [
                new DetalleVenta
                {
                    ProductoId = producto.IdProducto,
                    Cantidad = dto.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = subtotal
                }
            ]
        };
        db.Ventas.Add(venta);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), new
        {
            venta.IdVenta,
            venta.Fecha,
            venta.Total,
            Producto = producto.Nombre,
            venta.Detalles[0].Cantidad
        });
    }
}
