using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.Productos.DTOs;
using Optica.Api.Modules.Productos.Models;

public class ProductoRepository : IProductoRepository
{
    private readonly OpticaDbContext _context;

    public ProductoRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<Producto> CrearConHistorialStock(Producto producto, int idResponsable)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        _context.HistorialStock.AddRange(
            new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = "Registro de nuevo producto",
                TipoStock = "Normal",
                ValorAnterior = 0,
                ValorActual = producto.Stock
            },
            new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = "Registro de nuevo producto",
                TipoStock = "Minimo",
                ValorAnterior = 0,
                ValorActual = producto.StockMinimo
            });
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        return producto;
    }

    public Task<bool> ExisteCodigo(string codigo) =>
        _context.Productos.AnyAsync(producto => producto.Codigo == codigo);

    public async Task<List<Producto>> Buscar(string termino)
    {
        var productos = await ConsultaProductos(termino).OrderBy(p => p.Nombre).ToListAsync();
        await MarcarProductosConVentas(productos);
        return productos;
    }

    public async Task<(List<Producto> Productos, int Total)> BuscarPagina(string termino, string estado, string categoria,
        string color, string marca, string ordenPrecio, int desplazamiento, int limite)
    {
        var consulta = ConsultaProductos(termino);
        if (estado.Length > 0) consulta = consulta.Where(p => p.Estado == estado);
        if (categoria.Length > 0) consulta = consulta.Where(p => p.Categoria == categoria);
        if (color.Length > 0) consulta = consulta.Where(p => p.Color == color);
        if (marca.Length > 0) consulta = consulta.Where(p => p.Marca == marca);
        var total = await consulta.CountAsync();
        var ordenada = ordenPrecio switch
        {
            "menor" => consulta.OrderBy(p => p.Precio).ThenBy(p => p.Nombre),
            "mayor" => consulta.OrderByDescending(p => p.Precio).ThenBy(p => p.Nombre),
            _ => consulta.OrderBy(p => p.Nombre)
        };
        var productos = await ordenada.ThenBy(p => p.IdProducto).Skip(desplazamiento).Take(limite).ToListAsync();
        await MarcarProductosConVentas(productos);
        return (productos, total);
    }

    public async Task<(string[] Categorias, string[] Colores, string[] Marcas)> OpcionesBusqueda(string termino)
    {
        var consulta = ConsultaProductos(termino);
        return (await Opciones(consulta.Select(p => p.Categoria)), await Opciones(consulta.Select(p => p.Color)),
            await Opciones(consulta.Select(p => p.Marca)));
    }

    private static Task<string[]> Opciones(IQueryable<string?> consulta) =>
        consulta.Where(valor => valor != null && valor != "").Select(valor => valor!).Distinct().OrderBy(valor => valor).ToArrayAsync();

    private IQueryable<Producto> ConsultaProductos(string termino)
    {
        var consulta = _context.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(termino))
        {
            var filtro = termino.Trim();
            consulta = consulta.Where(producto => producto.Nombre.Contains(filtro) || producto.Categoria.Contains(filtro) || producto.Codigo.Contains(filtro));
        }

        return consulta;
    }

    public Task<List<Producto>> ObtenerParaReporteInventario(CancellationToken cancellationToken) =>
        _context.Productos
            .AsNoTracking()
            .OrderBy(producto => producto.Nombre)
            .ThenBy(producto => producto.Codigo)
            .ToListAsync(cancellationToken);

    public async Task<Producto?> ObtenerPorId(int id)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(item => item.IdProducto == id);
        if (producto is not null)
        {
            await MarcarProductosConVentas([producto]);
        }

        return producto;
    }

    private async Task MarcarProductosConVentas(List<Producto> productos)
    {
        if (productos.Count == 0)
            return;

        var ids = productos.Select(producto => producto.IdProducto).ToList();
        var idsConVentas = await _context.DetallesVenta
            .Where(detalle => detalle.ProductoId.HasValue && ids.Contains(detalle.ProductoId.Value))
            .Select(detalle => detalle.ProductoId!.Value)
            .Distinct()
            .ToListAsync();

        var productosVendidos = idsConVentas.ToHashSet();
        foreach (var producto in productos)
        {
            producto.TieneVentas = productosVendidos.Contains(producto.IdProducto);
        }
    }

    public async Task<Producto> ActualizarConHistorialStock(Producto producto, int idResponsable, string motivoMovimiento)
    {
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        var productosBloqueados = await _context.Productos
            .FromSqlInterpolated($"SELECT * FROM productos WHERE id_producto = {producto.IdProducto} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync();
        var valoresAnteriores = productosBloqueados.SingleOrDefault()
            ?? throw new KeyNotFoundException("No se encuentra el producto.");

        var stockCambio = valoresAnteriores.Stock != producto.Stock;
        var stockMinimoCambio = valoresAnteriores.StockMinimo != producto.StockMinimo;
        if ((stockCambio || stockMinimoCambio) && string.IsNullOrWhiteSpace(motivoMovimiento))
        {
            throw new ArgumentException("El motivo es obligatorio cuando cambia el stock.");
        }

        _context.Entry(producto).OriginalValues.SetValues(valoresAnteriores);
        _context.Productos.Update(producto);

        if (stockCambio)
        {
            _context.HistorialStock.Add(new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = motivoMovimiento,
                TipoStock = "Normal",
                ValorAnterior = valoresAnteriores.Stock,
                ValorActual = producto.Stock
            });
        }

        if (stockMinimoCambio)
        {
            _context.HistorialStock.Add(new HistorialStock
            {
                IdProducto = producto.IdProducto,
                IdResponsable = idResponsable,
                MotivoMovimiento = motivoMovimiento,
                TipoStock = "Minimo",
                ValorAnterior = valoresAnteriores.StockMinimo,
                ValorActual = producto.StockMinimo
            });
        }

        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
        await MarcarProductosConVentas([producto]);
        return producto;
    }

    public async Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(
        int idProducto,
        int pagina,
        int tamanoPagina)
    {
        if (!await _context.Productos.AsNoTracking().AnyAsync(producto => producto.IdProducto == idProducto))
        {
            return null;
        }

        var consulta = _context.HistorialStock.AsNoTracking()
            .Where(historial => historial.IdProducto == idProducto);
        var totalRegistros = await consulta.CountAsync();
        var movimientos = await (
            from historial in consulta
            join responsable in _context.Trabajadores.AsNoTracking()
                on historial.IdResponsable equals responsable.IdTrabajador
            orderby historial.FechaCambio descending, historial.IdCambio descending
            select new HistorialStockMovimientoResponseDto(
                historial.IdCambio,
                historial.IdProducto,
                historial.MotivoMovimiento,
                historial.IdResponsable,
                responsable.Nombre,
                historial.TipoStock,
                historial.ValorAnterior,
                historial.ValorActual,
                historial.FechaCambio))
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return new HistorialStockPaginadoResponseDto(
            pagina,
            tamanoPagina,
            totalRegistros,
            (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            movimientos);
    }

    public async Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(
        int pagina,
        int tamanoPagina)
    {
        var consulta = _context.HistorialStock.AsNoTracking();
        var totalRegistros = await consulta.CountAsync();
        var paginaHistorial = consulta
            .OrderByDescending(historial => historial.FechaCambio)
            .ThenByDescending(historial => historial.IdCambio)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina);

        var movimientos = await (
            from historial in paginaHistorial
            join responsable in _context.Trabajadores.AsNoTracking()
                on historial.IdResponsable equals responsable.IdTrabajador
            orderby historial.FechaCambio descending, historial.IdCambio descending
            select new HistorialStockMovimientoResponseDto(
                historial.IdCambio,
                historial.IdProducto,
                historial.MotivoMovimiento,
                historial.IdResponsable,
                responsable.Nombre,
                historial.TipoStock,
                historial.ValorAnterior,
                historial.ValorActual,
                historial.FechaCambio))
            .ToListAsync();

        return new HistorialStockPaginadoResponseDto(
            pagina,
            tamanoPagina,
            totalRegistros,
            (int)Math.Ceiling(totalRegistros / (double)tamanoPagina),
            movimientos);
    }

    public async Task<bool> Eliminar(int id)
    {
        var producto = await _context.Productos.FirstOrDefaultAsync(item => item.IdProducto == id);
        if (producto is null)
        {
            return false;
        }

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return true;
    }
}