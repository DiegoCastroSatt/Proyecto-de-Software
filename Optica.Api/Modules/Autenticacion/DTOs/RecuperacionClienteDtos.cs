using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record SolicitarRecuperacionClienteRequest(string Correo);

public sealed record ConfirmarRecuperacionClienteRequest(
    string Token,
    string NuevaContrasena,
    string ConfirmarContrasena);

public sealed record ConfirmarCuentaClienteRequest(string Token);

public sealed record ReenviarConfirmacionClienteRequest(string Correo);

public sealed class SolicitarCambioCorreoClienteRequest
{
    [Required, EmailAddress, StringLength(100)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;
}
