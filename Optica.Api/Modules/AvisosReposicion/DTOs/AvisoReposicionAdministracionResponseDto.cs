namespace Optica.Api.Modules.AvisosReposicion.DTOs;

public sealed record AvisoReposicionAdministracionResponseDto(
    int IdAviso,
    string RutCliente,
    string NombreCliente,
    string? CorreoCliente,
    int IdProducto,
    string NombreProducto,
    DateTimeOffset Fecha,
    string Estado);
