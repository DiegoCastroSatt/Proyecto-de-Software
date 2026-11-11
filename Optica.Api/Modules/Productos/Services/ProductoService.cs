using Optica.Api.Modules.Marcos;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.Productos.DTOs;
using Optica.Api.Modules.Productos.Models;

public class ProductoService : IProductoService
{
    private static readonly string[] EstadosValidos = ["Disponible", "Agotado"];
    private static readonly string[] ExtensionesImagenValidas = [".jpg", ".jpeg", ".png", ".webp"];
    private readonly IProductoRepository _productoRepository;
    private readonly IWebHostEnvironment _environment;

    public ProductoService(IProductoRepository productoRepository, IWebHostEnvironment environment)
    {
        _productoRepository = productoRepository;
        _environment = environment;
    }

    public async Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto, int idResponsable)
    {
        if (idResponsable <= 0)
        {
            throw new ArgumentException("No se pudo identificar al responsable del registro.");
        }

        var codigo = dto.Codigo.Trim();
        var nombre = dto.Nombre.Trim();
        var estado = dto.Estado.Trim();

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(dto.Categoria))
        {
            throw new ArgumentException("El código, nombre y categoría son obligatorios.");
        }

        if (codigo.Length > 30 || nombre.Length > 100 || dto.Marca.Trim().Length > 60 || dto.Modelo.Trim().Length > 60 || dto.Color.Trim().Length > 40 || dto.Categoria.Trim().Length > 50)
        {
            throw new ArgumentException("Uno o más campos superan la longitud permitida.");
        }

        if (dto.Precio < 0 || dto.Stock < 0 || dto.StockMinimo < 0)
        {
            throw new ArgumentException("El precio y las cantidades no pueden ser negativos.");
        }

        if (dto.Precio > 99999999.99m)
        {
            throw new ArgumentException("El precio supera el máximo permitido.");
        }

        if (!EstadosValidos.Contains(estado, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El estado debe ser Disponible o Agotado.");
        }

        if (await _productoRepository.ExisteCodigo(codigo))
        {
            throw new InvalidOperationException("Ya existe un producto con ese código.");
        }

        var rutaImagen = await GuardarImagen(dto.Imagen);

        var producto = await _productoRepository.CrearConHistorialStock(new Producto
        {
            Codigo = codigo,
            Nombre = nombre,
            Marca = dto.Marca.Trim(),
            Modelo = dto.Modelo.Trim(),
            Color = dto.Color.Trim(),
            Categoria = dto.Categoria.Trim(),
            Precio = dto.Precio,
            Stock = dto.Stock,
            StockMinimo = dto.StockMinimo,
            Estado = EstadosValidos.First(estadoValido => string.Equals(estadoValido, estado, StringComparison.OrdinalIgnoreCase)),
            RutaImagen = rutaImagen
        }, idResponsable);

        return Mapear(producto);
    }

    public async Task<List<ProductoResponseDto>> BuscarProductos(string termino) =>
        (await _productoRepository.Buscar(termino)).Select(Mapear).ToList();

    public async Task<ReporteInventarioResponseDto> ConsultarReporteInventario(CancellationToken cancellationToken)
    {
        var productos = await _productoRepository.ObtenerParaReporteInventario(cancellationToken);
        var agotados = productos
            .Where(producto => producto.Stock <= 0)
            .Select(MapearProductoReporte)
            .ToList();
        var stockBajo = productos
            .Where(producto => producto.Stock > 0 && producto.Stock <= producto.StockMinimo)
            .Select(MapearProductoReporte)
            .ToList();
        var disponibles = productos
            .Where(producto => producto.Stock > producto.StockMinimo)
            .Select(MapearProductoReporte)
            .ToList();

        return new ReporteInventarioResponseDto(
            DateTimeOffset.UtcNow,
            productos.Count,
            disponibles.Count,
            agotados.Count,
            stockBajo.Count,
            disponibles,
            agotados,
            stockBajo);
    }

    public async Task<ProductoResponseDto?> ObtenerProducto(int id)
    {
        var producto = await _productoRepository.ObtenerPorId(id);
        return producto is null ? null : Mapear(producto);
    }

    public async Task<ProductoResponseDto> ActualizarProducto(int id, ActualizarProductoDto dto, int idResponsable)
    {
        ValidarResponsable(idResponsable);
        var producto = await _productoRepository.ObtenerPorId(id) ?? throw new KeyNotFoundException("No se encuentra el producto.");
        var codigo = dto.Codigo.Trim();
        var nombre = dto.Nombre.Trim();
        var categoria = dto.Categoria.Trim();
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(categoria))
        {
            throw new ArgumentException("El código, nombre y categoría son obligatorios.");
        }

        if (dto.Precio < 0 || dto.Stock < 0 || dto.StockMinimo < 0 || !EstadosValidos.Contains(dto.Estado.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Los datos del producto no son válidos.");
        }

        var cambiaStock = producto.Stock != dto.Stock || producto.StockMinimo != dto.StockMinimo;
        if (cambiaStock)
        {
            ValidarMotivo(dto.MotivoCambio);
        }

        producto.Codigo = codigo;
        producto.Nombre = nombre;
        producto.Marca = dto.Marca.Trim();
        producto.Modelo = dto.Modelo.Trim();
        producto.Color = dto.Color.Trim();
        producto.Categoria = categoria;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.StockMinimo = dto.StockMinimo;
        producto.Estado = EstadosValidos.First(estado => string.Equals(estado, dto.Estado.Trim(), StringComparison.OrdinalIgnoreCase));
        string? rutaImagenAnterior = null;
        if (dto.Imagen is not null && dto.Imagen.Length > 0)
        {
            rutaImagenAnterior = producto.RutaImagen;
            producto.RutaImagen = await GuardarImagen(dto.Imagen);
        }

        var actualizado = await _productoRepository.ActualizarConHistorialStock(
            producto,
            idResponsable,
            dto.MotivoCambio?.Trim() ?? string.Empty);
        if (rutaImagenAnterior is not null)
        {
            EliminarImagen(rutaImagenAnterior);
        }

        return Mapear(actualizado);
    }

    public async Task<ProductoResponseDto> ActualizarStock(int id, ActualizarStockDto dto, int idResponsable)
    {
        ValidarResponsableYMotivo(idResponsable, dto.MotivoCambio);
        if (dto.Stock < 0 || dto.StockMinimo < 0)
        {
            throw new ArgumentException("Las cantidades de stock no pueden ser negativas.");
        }

        var producto = await _productoRepository.ObtenerPorId(id)
            ?? throw new KeyNotFoundException("No se encuentra el producto.");

        producto.Stock = dto.Stock;
        producto.StockMinimo = dto.StockMinimo;
        producto.Estado = dto.Stock > 0 ? "Disponible" : "Agotado";
        return Mapear(await _productoRepository.ActualizarConHistorialStock(
            producto,
            idResponsable,
            dto.MotivoCambio.Trim()));
    }

    public async Task<HistorialStockPaginadoResponseDto?> ConsultarHistorialStock(
        int idProducto,
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacionHistorial(pagina, tamanoPagina);
        if (idProducto <= 0)
            throw new ArgumentException("El identificador del producto no es válido.");

        return await _productoRepository.ConsultarHistorialStock(idProducto, pagina, tamanoPagina);
    }

    public async Task<HistorialStockPaginadoResponseDto> ConsultarHistorialStockReciente(
        int pagina,
        int tamanoPagina)
    {
        ValidarPaginacionHistorial(pagina, tamanoPagina);
        return await _productoRepository.ConsultarHistorialStockReciente(pagina, tamanoPagina);
    }

    private static void ValidarPaginacionHistorial(int pagina, int tamanoPagina)
    {
        if (pagina <= 0 || tamanoPagina <= 0 || tamanoPagina > 100 ||
            (long)(pagina - 1) * tamanoPagina > int.MaxValue)
        {
            throw new ArgumentException("La paginación solicitada no es válida.");
        }
    }

    public async Task EliminarProducto(int id)
    {
        var producto = await _productoRepository.ObtenerPorId(id)
            ?? throw new KeyNotFoundException("No se encuentra el producto.");

        try
        {
            if (!await _productoRepository.Eliminar(id))
            {
                throw new KeyNotFoundException("No se encuentra el producto.");
            }
        }
        catch (DbUpdateException ex) when (
            ex.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("ventas", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("detalle_venta", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("asociado", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se puede eliminar el producto porque está asociado a ventas o detalles de venta.", ex);
        }

        EliminarImagen(producto.RutaImagen);
    }

    private static ProductoResponseDto Mapear(Producto producto) => new()
    {
        Id = producto.IdProducto,
        IdTipoMarco = producto.IdTipoMarco,
        Codigo = producto.Codigo,
        Nombre = producto.Nombre,
        Marca = producto.Marca,
        Modelo = producto.Modelo,
        Color = producto.Color,
        Categoria = producto.Categoria,
        Precio = producto.Precio,
        Stock = producto.Stock,
        StockMinimo = producto.StockMinimo,
        Estado = producto.Estado,
        RutaImagen = producto.RutaImagen,
        TieneVentas = producto.TieneVentas
    };

    private static ProductoReporteInventarioDto MapearProductoReporte(Producto producto) =>
        new(
            producto.IdProducto,
            producto.Codigo,
            producto.Nombre,
            producto.Categoria,
            producto.Stock,
            producto.StockMinimo);

    private static void ValidarResponsableYMotivo(int idResponsable, string? motivo)
    {
        ValidarResponsable(idResponsable);
        ValidarMotivo(motivo);
    }

    private static void ValidarResponsable(int idResponsable)
    {
        if (idResponsable <= 0)
            throw new ArgumentException("No se pudo identificar al responsable del cambio.");
    }

    private static void ValidarMotivo(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 255)
            throw new ArgumentException("El motivo es obligatorio y no puede superar los 255 caracteres.");
    }

    private async Task<string?> GuardarImagen(IFormFile? imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        if (imagen.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("La imagen no puede superar los 5 MB.");
        }

        var extension = Path.GetExtension(imagen.FileName).ToLowerInvariant();
        if (!ExtensionesImagenValidas.Contains(extension) || !imagen.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La imagen debe ser JPG, PNG o WebP.");
        }

        var directorio = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "productos");
        Directory.CreateDirectory(directorio);

        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var rutaFisica = Path.Combine(directorio, nombreArchivo);

        await using var stream = File.Create(rutaFisica);
        await imagen.CopyToAsync(stream);
        return $"/uploads/productos/{nombreArchivo}";
    }

    private void EliminarImagen(string? rutaImagen)
    {
        if (string.IsNullOrWhiteSpace(rutaImagen))
        {
            return;
        }

        var nombreArchivo = Path.GetFileName(rutaImagen);
        var directorio = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads", "productos");
        var rutaFisica = Path.Combine(directorio, nombreArchivo);
        if (File.Exists(rutaFisica))
        {
            File.Delete(rutaFisica);
        }
    }
}