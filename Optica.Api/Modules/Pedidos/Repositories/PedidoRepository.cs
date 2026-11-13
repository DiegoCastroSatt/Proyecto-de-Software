using Optica.Api.Modules.Clientes.Services;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;

namespace Optica.Api.Modules.Pedidos.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly OpticaDbContext _context;

    public PedidoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pedido>> ObtenerPedidosAsync()
    {
        return await _context.Pedidos
            .OrderByDescending(p => p.Fecha)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync() =>
        await ConsultarPedidosDetalle().OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.IdPedido).ToListAsync();

    public async Task<PedidosPaginaResponseDto> ObtenerPaginaAsync(int pagina, int tamano, string termino, DateTime hoy)
    {
        var consulta = ConsultarPedidosDetalle();
        if (termino.Length > 0)
            consulta = consulta.Where(p => p.NombreCliente.Contains(termino));

        var vencidos = consulta.Where(p => p.FechaEntregaEstimada.HasValue
            && p.FechaEntregaEstimada < hoy && p.Estado != "Entregado" && p.Estado != "Cancelado");
        var vigentes = consulta.Where(p => !p.FechaEntregaEstimada.HasValue
            || p.FechaEntregaEstimada >= hoy || p.Estado == "Entregado" || p.Estado == "Cancelado");
        var desplazamiento = (pagina - 1) * tamano;
        var total = await consulta.CountAsync();
        var limite = tamano;
        var pedidos = await OrdenarPagina(vencidos, desplazamiento, limite).ToListAsync();
        if (pedidos.Count < limite)
        {
            // Si la página empieza después de los vencidos, trasladar solo el desplazamiento restante.
            var omitidos = pedidos.Count == 0 && desplazamiento > 0 ? await vencidos.CountAsync() : desplazamiento;
            pedidos.AddRange(await OrdenarPagina(vigentes, desplazamiento - omitidos, limite - pedidos.Count).ToListAsync());
        }

        var totalPaginas = (int)Math.Ceiling(total / (double)tamano);
        return new PedidosPaginaResponseDto(pedidos, pagina < totalPaginas, totalPaginas);
    }

    private static IQueryable<PedidoResponseDto> OrdenarPagina(IQueryable<PedidoResponseDto> consulta, int desplazamiento, int limite) =>
        consulta.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.IdPedido)
            .Skip(desplazamiento).Take(limite);

    private IQueryable<PedidoResponseDto> ConsultarPedidosDetalle() =>
        _context.Pedidos.AsNoTracking()
            .Join(
                _context.Clientes,
                p => p.Rut,
                c => c.Rut,
                (p, c) => new PedidoResponseDto
                {
                    IdPedido = p.IdPedido,
                    NombreCliente = c.Nombre + " " + c.Apellido,
                    Fecha = p.Fecha,
                    Estado = p.Estado,
                    Total = p.Total,
                    Anotaciones = p.Anotaciones,
                    FechaEntregaEstimada = p.FechaEntregaEstimada
                });

    public async Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync()
    {
        return await _context.Clientes
            .Where(c => c.Estado == "Activo")
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cliente>> BuscarClientesPorRutAsync(string rut)
    {
        return await _context.Clientes
            .Where(c => c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper().Contains(rut))
            .OrderBy(c => c.Rut)
            .Take(10)
            .ToListAsync();
    }

    public async Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut)
    {
        rut = RutChilenoValidator.Normalizar(rut);
        return await _context.Clientes
            .Where(c => c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut && c.Estado == "Activo")
            .OrderBy(c => c.IdCliente)
            .FirstOrDefaultAsync();
    }

    public async Task<Cliente?> ObtenerClientePorRutAsync(string rut)
    {
        rut = RutChilenoValidator.Normalizar(rut);
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut);
    }

    public async Task<Pedido> CrearPedidoAsync(Pedido pedido)
    {
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();
        return pedido;
    }

    public async Task<Pedido?> ObtenerPedidoPorIdAsync(int id)
    {
        return await _context.Pedidos.FindAsync(id);
    }

    public Task<Pedido?> ObtenerPedidoPorIdParaActualizarAsync(int id) =>
        _context.Pedidos
            .FromSqlInterpolated($"SELECT id_pedido, rut, id_receta, fecha, estado, total, anotaciones, fecha_entrega_estimada FROM pedidos WHERE id_pedido = {id} FOR UPDATE")
            .SingleOrDefaultAsync();

    public async Task ActualizarPedidoAsync(Pedido pedido)
    {
        _context.Pedidos.Update(pedido);
        await _context.SaveChangesAsync();
    }

    public async Task<PedidoResponseDto?> ObtenerPedidoDetalleAsync(int id)
    {
        return await _context.Pedidos
            .Where(p => p.IdPedido == id)
            .Join(
                _context.Clientes,
                p => p.Rut,
                c => c.Rut,
                (p, c) => new PedidoResponseDto
                {
                    IdPedido = p.IdPedido,
                    NombreCliente = c.Nombre + " " + c.Apellido,
                    Fecha = p.Fecha,
                    Estado = p.Estado,
                    Total = p.Total,
                    Anotaciones = p.Anotaciones,
                    FechaEntregaEstimada = p.FechaEntregaEstimada
                }
            )
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetallePorRutAsync(string rut)
    {
        return await _context.Pedidos
            .AsNoTracking()
            .Where(p => p.Rut == rut)
            .Join(
                _context.Clientes,
                p => p.Rut,
                c => c.Rut,
                (p, c) => new PedidoResponseDto
                {
                    IdPedido = p.IdPedido,
                    NombreCliente = c.Nombre + " " + c.Apellido,
                    Fecha = p.Fecha,
                    Estado = p.Estado,
                    Total = p.Total,
                    Anotaciones = p.Anotaciones,
                    FechaEntregaEstimada = p.FechaEntregaEstimada
                })
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.IdPedido)
            .ToListAsync();
    }
}