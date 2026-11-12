namespace Optica.Api.Modules.Encuestas.DTOs;

/// <summary>Datos que ve el cliente al abrir el enlace. No incluye datos personales.</summary>
public sealed record EncuestaPublicaDto(string FechaAtencion, string HoraInicio);
