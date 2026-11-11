namespace Optica.Api.Modules.Productos.DTOs;

/// <summary>Datos de stock de un producto incluidos en el reporte de inventario.</summary>
public sealed record ProductoReporteInventarioDto(
    int IdProducto,
    string Codigo,
    string Nombre,
    string Categoria,
    int Stock,
    int StockMinimo);

/// <summary>Reporte de inventario agrupado por disponibilidad y con sus totales.</summary>
public sealed record ReporteInventarioResponseDto(
    DateTimeOffset GeneradoEn,
    int TotalProductos,
    int TotalDisponibles,
    int TotalAgotados,
    int TotalStockBajo,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosDisponibles,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosAgotados,
    IReadOnlyList<ProductoReporteInventarioDto> ProductosStockBajo);
