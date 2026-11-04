namespace Optica.Api.Modules.Productos.DTOs;

public sealed record HistorialStockMovimientoResponseDto(
    int IdCambio,
    int IdProducto,
    string MotivoMovimiento,
    int IdResponsable,
    string NombreResponsable,
    string TipoStock,
    int ValorAnterior,
    int ValorActual,
    DateTime FechaCambio);

public sealed record HistorialStockPaginadoResponseDto(
    int Pagina,
    int TamanoPagina,
    int TotalRegistros,
    int TotalPaginas,
    IReadOnlyList<HistorialStockMovimientoResponseDto> Movimientos);
