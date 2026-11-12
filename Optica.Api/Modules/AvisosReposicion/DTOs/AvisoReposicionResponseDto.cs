namespace Optica.Api.Modules.AvisosReposicion.DTOs;

public sealed record AvisoReposicionResponseDto(
    int IdAviso,
    int IdProducto,
    string NombreProducto,
    DateTimeOffset Fecha,
    string Estado);
