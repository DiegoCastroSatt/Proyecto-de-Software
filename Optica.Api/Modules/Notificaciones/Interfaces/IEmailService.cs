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

    async Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
        string? destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken,
        bool importante = false,
        byte[]? adjunto = null,
        string? nombreAdjunto = null,
        string? tipoAdjunto = null)
    {
        var exitoso = await EnviarAsync(destinatario, asunto, cuerpoHtml, cancellationToken,
            importante, adjunto, nombreAdjunto, tipoAdjunto);
        return new ResultadoEnvioCorreo(exitoso, exitoso ? null : "El servicio de correo no confirmó el envío.");
    }
}
