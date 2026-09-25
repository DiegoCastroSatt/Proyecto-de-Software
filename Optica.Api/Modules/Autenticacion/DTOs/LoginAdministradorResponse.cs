namespace Optica.Api.Modules.Autenticacion.DTOs;

/// <summary>Datos públicos del administrador autenticado.</summary>
public sealed record LoginAdministradorResponse(
    int IdAdministrador,
    string Nombre);