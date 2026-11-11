using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AvisosReposicion.DTOs;
using Optica.Api.Modules.AvisosReposicion.Interfaces;
using Optica.Api.Modules.AvisosReposicion.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

namespace Optica.Api.Modules.AvisosReposicion.Repositories;

public sealed class AvisoReposicionRepository(OpticaDbContext db) : IAvisoReposicionRepository
{
    public Task<Cliente?> ObtenerCliente(int idCliente, CancellationToken cancellationToken) =>
        db.Clientes.AsNoTracking().SingleOrDefaultAsync(
            cliente => cliente.IdCliente == idCliente && cliente.Estado == "Activo",
            cancellationToken);

    public Task<Producto?> ObtenerProducto(int idProducto, CancellationToken cancellationToken) =>
        db.Productos.AsNoTracking().SingleOrDefaultAsync(
            producto => producto.IdProducto == idProducto,
            cancellationToken);

    public Task<bool> ExisteAvisoAbierto(string rut, int idProducto, CancellationToken cancellationToken) =>
        db.AvisosReposicion.AnyAsync(
            aviso => aviso.Rut == rut && aviso.IdProducto == idProducto && aviso.Estado == "Abierta",
            cancellationToken);

    public async Task<AvisoReposicion> Crear(AvisoReposicion aviso, CancellationToken cancellationToken)
    {
        db.AvisosReposicion.Add(aviso);
        await db.SaveChangesAsync(cancellationToken);
        return aviso;
    }

    public async Task<IReadOnlyList<AvisoReposicionResponseDto>> ListarPorCliente(
        int idCliente,
        CancellationToken cancellationToken)
    {
        var resultados = await (
            from aviso in db.AvisosReposicion.AsNoTracking()
            join cliente in db.Clientes.AsNoTracking() on aviso.Rut equals cliente.Rut
            join producto in db.Productos.AsNoTracking() on aviso.IdProducto equals producto.IdProducto
            where cliente.IdCliente == idCliente
            orderby aviso.Fecha descending
            select new
            {
                aviso.IdAviso,
                aviso.IdProducto,
                NombreProducto = producto.Nombre,
                aviso.Fecha,
                aviso.Estado
            }).ToListAsync(cancellationToken);

        return resultados.Select(aviso => new AvisoReposicionResponseDto(
            aviso.IdAviso, aviso.IdProducto, aviso.NombreProducto, ComoFechaUtc(aviso.Fecha), aviso.Estado)).ToList();
    }

    public async Task<IReadOnlyList<AvisoReposicionAdministracionResponseDto>> ListarTodas(
        CancellationToken cancellationToken)
    {
        var resultados = await ConsultaAdministracion()
            .OrderByDescending(aviso => aviso.Fecha)
            .ThenByDescending(aviso => aviso.IdAviso)
            .ToListAsync(cancellationToken);

        return resultados.Select(MapearAdministracion).ToList();
    }

    public async Task<IReadOnlyList<AvisoReposicionAdministracionResponseDto>> ListarPendientesConStock(
        CancellationToken cancellationToken)
    {
        var resultados = await ConsultaAdministracion()
            .Where(aviso => aviso.Estado == "Abierta" && aviso.Stock > 0)
            .OrderBy(aviso => aviso.Fecha)
            .ThenBy(aviso => aviso.IdAviso)
            .Take(50)
            .ToListAsync(cancellationToken);

        return resultados.Select(MapearAdministracion).ToList();
    }

    public async Task Cerrar(int idAviso, CancellationToken cancellationToken)
    {
        await db.AvisosReposicion
            .Where(aviso => aviso.IdAviso == idAviso && aviso.Estado == "Abierta"
                && aviso.Producto.Stock > 0)
            .ExecuteUpdateAsync(update => update.SetProperty(aviso => aviso.Estado, "Cerrada"), cancellationToken);
    }

    private IQueryable<AvisoAdministracionProyeccion> ConsultaAdministracion() =>
        from aviso in db.AvisosReposicion.AsNoTracking()
        join cliente in db.Clientes.AsNoTracking() on aviso.Rut equals cliente.Rut
        join producto in db.Productos.AsNoTracking() on aviso.IdProducto equals producto.IdProducto
        select new AvisoAdministracionProyeccion
        {
            IdAviso = aviso.IdAviso,
            RutCliente = aviso.Rut,
            NombreCliente = cliente.Nombre + " " + cliente.Apellido,
            CorreoCliente = cliente.Correo,
            IdProducto = aviso.IdProducto,
            NombreProducto = producto.Nombre,
            Fecha = aviso.Fecha,
            Estado = aviso.Estado,
            Stock = producto.Stock
        };

    private static AvisoReposicionAdministracionResponseDto MapearAdministracion(AvisoAdministracionProyeccion aviso) =>
        new(aviso.IdAviso, aviso.RutCliente, aviso.NombreCliente, aviso.CorreoCliente,
            aviso.IdProducto, aviso.NombreProducto, ComoFechaUtc(aviso.Fecha), aviso.Estado);

    private static DateTimeOffset ComoFechaUtc(DateTime fecha) =>
        new(DateTime.SpecifyKind(fecha, DateTimeKind.Utc));

    private sealed class AvisoAdministracionProyeccion
    {
        public int IdAviso { get; init; }
        public string RutCliente { get; init; } = string.Empty;
        public string NombreCliente { get; init; } = string.Empty;
        public string? CorreoCliente { get; init; }
        public int IdProducto { get; init; }
        public string NombreProducto { get; init; } = string.Empty;
        public DateTime Fecha { get; init; }
        public string Estado { get; init; } = string.Empty;
        public int Stock { get; init; }
    }
}
