namespace Optica.Api.Modules.AvisosReposicion.DTOs;

/// <summary>Representa una solicitud de reposición para su consulta administrativa.</summary>
public sealed record AvisoReposicionAdministracionResponseDto(
    int IdAviso,
    string RutCliente,
    string NombreCliente,
    string? CorreoCliente,
    int IdProducto,
    string NombreProducto,
    DateTimeOffset Fecha,
    string Estado);
