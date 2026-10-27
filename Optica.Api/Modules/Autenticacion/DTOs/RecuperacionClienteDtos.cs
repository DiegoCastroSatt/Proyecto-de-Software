namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record SolicitarRecuperacionClienteRequest(string Correo);

public sealed record ConfirmarRecuperacionClienteRequest(
    string Token,
    string NuevaContrasena,
    string ConfirmarContrasena);