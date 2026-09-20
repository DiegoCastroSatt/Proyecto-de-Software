using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
namespace Optica.Api.Modules.Ventas.Repositories;

public class ConsultaProductoVenta(OpticaDbContext db) : IConsultaProductoVenta
{
    public Task<ProductoCajaDto?> Buscar(string codigo) => db.Productos.AsNoTracking()
        .Where(p => p.Codigo == codigo.Trim())
        .Select(p => new ProductoCajaDto(p.IdProducto, p.Codigo, p.Nombre, p.Precio))
        .SingleOrDefaultAsync();
}
