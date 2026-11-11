namespace Optica.Api.Modules.AvisosReposicion.DTOs;

/// <summary>Representa una solicitud de aviso de reposición del cliente.</summary>
public sealed record AvisoReposicionResponseDto(
    int IdAviso,
    int IdProducto,
    string NombreProducto,
    DateTimeOffset Fecha,
    string Estado);
