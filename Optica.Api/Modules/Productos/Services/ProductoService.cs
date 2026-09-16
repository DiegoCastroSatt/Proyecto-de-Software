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

    public async Task<ProductoResponseDto> CrearProducto(CrearProductoDto dto)
    {
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

        var producto = await _productoRepository.Crear(new Producto
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
        });

        return new ProductoResponseDto
        {
            Id = producto.IdProducto,
            Codigo = producto.Codigo,
            Nombre = producto.Nombre,
            Estado = producto.Estado,
            RutaImagen = producto.RutaImagen
        };
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
}