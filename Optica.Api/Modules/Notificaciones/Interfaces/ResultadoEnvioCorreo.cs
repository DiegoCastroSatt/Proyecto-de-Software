namespace Optica.Api.Modules.Notificaciones.Interfaces;

public sealed record ResultadoEnvioCorreo(bool Exitoso, string? Error = null);
