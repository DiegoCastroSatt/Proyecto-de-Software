namespace Optica.Api.Modules.Resenas.DTOs;

public sealed record ResenaResponseDto(int Id, int IdProducto, int Calificacion, string Comentario,
    string NombreCliente, DateTime FechaCreacionUtc, DateTime? FechaActualizacionUtc);

public sealed record ResumenResenasDto(double Promedio, int Cantidad);

public sealed record ProductoResenableDto(int IdProducto, bool YaResenado);
