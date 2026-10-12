namespace Optica.Api.Modules.Notificaciones.Interfaces;

public interface IEmailService
{
    Task<bool> EnviarAsync(
        string? destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken,
        bool importante = false,
        byte[]? adjunto = null,
        string? nombreAdjunto = null,
        string? tipoAdjunto = null);
}
