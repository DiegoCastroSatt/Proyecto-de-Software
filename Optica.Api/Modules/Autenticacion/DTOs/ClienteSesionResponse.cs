namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record ClienteSesionResponse(int IdCliente, string Nombre, string Correo, string Rut = "");
