namespace Optica.Api.Modules.Ventas.DTOs;

public record DetalleVentaResponseDto(int? ProductoId, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
public record ProductoHistorialDto(int? ProductoId, string? Nombre, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
public record VentaResponseDto(int IdVenta, DateTime Fecha, decimal Total, IReadOnlyList<ProductoHistorialDto> Productos);
public record VentaCreadaResponseDto(int IdVenta, DateTime Fecha, decimal Total, IReadOnlyList<DetalleVentaResponseDto> Productos);
public record ProductoCajaDto(int IdProducto, string CodigoProducto, string Nombre, decimal Precio);
public record ErrorVentaDto(string Mensaje);
