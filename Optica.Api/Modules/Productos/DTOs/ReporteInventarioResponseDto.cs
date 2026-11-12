namespace Optica.Api.Modules.Productos.DTOs;


public sealed record ProductoReporteInventarioDto(
    int IdProducto,
    string Codigo,
    string Nombre,
    string Categoria,
    int Stock,
    int StockMinimo);

public sealed record ReporteInventarioResponseDto(
    DateTimeOffset GeneradoEn,
    int TotalProductos,
    int TotalDisponibles,
    int TotalAgotados,
    int TotalStockBajo,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosDisponibles,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosAgotados,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosStockBajo);
