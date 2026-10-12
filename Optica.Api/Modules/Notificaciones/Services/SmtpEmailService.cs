using System.Net;
using System.Net.Mail;
using System.Text;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Notificaciones.Services;

public sealed class SmtpEmailService(IConfiguration configuracion, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task<bool> EnviarAsync(
        string? destinatario,
        string asunto,
        string cuerpoHtml,
        CancellationToken cancellationToken,
        bool importante = false,
        byte[]? adjunto = null,
        string? nombreAdjunto = null,
        string? tipoAdjunto = null)
    {
        var host = configuracion["Correo:SmtpHost"];
        var remitente = configuracion["Correo:Remitente"];
        if (string.IsNullOrWhiteSpace(destinatario) || !MailAddress.TryCreate(destinatario, out _)
            || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(remitente))
        {
            logger.LogWarning("No se envió el correo: falta destinatario válido o configuración SMTP.");
            return false;
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(remitente, configuracion["Correo:NombreRemitente"] ?? "Centro Óptico San Francisco"),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                Priority = importante ? MailPriority.High : MailPriority.Normal
            };
            mensaje.To.Add(destinatario);
            if (adjunto is not null && !string.IsNullOrWhiteSpace(nombreAdjunto))
                mensaje.Attachments.Add(new Attachment(new MemoryStream(adjunto), nombreAdjunto, tipoAdjunto ?? "application/octet-stream"));

            using var smtp = new SmtpClient(host, int.TryParse(configuracion["Correo:Puerto"], out var puerto) ? puerto : 587)
            {
                EnableSsl = !bool.TryParse(configuracion["Correo:UsarTls"], out var usarTls) || usarTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };
            var usuario = configuracion["Correo:Usuario"];
            if (!string.IsNullOrWhiteSpace(usuario))
                smtp.Credentials = new NetworkCredential(usuario, configuracion["Correo:Clave"] ?? string.Empty);

            await smtp.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló SMTP al enviar correo ({TipoError}).", ex.GetType().Name);
            return false;
        }
    }
}
