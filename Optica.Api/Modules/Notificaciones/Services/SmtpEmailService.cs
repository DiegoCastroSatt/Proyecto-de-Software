using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.Notificaciones.Services;

public sealed class SmtpEmailService(
    IConfiguration configuracion,
    ILogger<SmtpEmailService> logger,
    IHttpClientFactory clienteHttpFactory) : IEmailService
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
        => (await EnviarConResultadoAsync(destinatario, asunto, cuerpoHtml, cancellationToken,
            importante, adjunto, nombreAdjunto, tipoAdjunto)).Exitoso;

    public async Task<ResultadoEnvioCorreo> EnviarConResultadoAsync(
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
        var apiKeyBrevo = configuracion["Correo:BrevoApiKey"];
        if (string.IsNullOrWhiteSpace(destinatario) || !MailAddress.TryCreate(destinatario, out _)
            || string.IsNullOrWhiteSpace(remitente)
            || (string.IsNullOrWhiteSpace(apiKeyBrevo) && string.IsNullOrWhiteSpace(host)))
        {
            logger.LogWarning("No se envió el correo: falta destinatario válido o configuración del proveedor.");
            return new ResultadoEnvioCorreo(false, "Destinatario inválido o configuración de correo incompleta.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(apiKeyBrevo))
            {
                // Implementación nueva con Brevo: se usa solo cuando se configura su clave API.
                return await EnviarConBrevoAsync(
                    apiKeyBrevo, remitente, destinatario, asunto, cuerpoHtml,
                    adjunto, nombreAdjunto, cancellationToken);
            }

            // Implementación anterior (SMTP real del proyecto): se conserva y sigue activa
            // como alternativa cuando no se configura la clave API de Brevo.
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
            return new ResultadoEnvioCorreo(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Falló SMTP al enviar correo ({TipoError}).", ex.GetType().Name);
            return new ResultadoEnvioCorreo(false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<ResultadoEnvioCorreo> EnviarConBrevoAsync(
        string apiKey,
        string remitente,
        string destinatario,
        string asunto,
        string cuerpoHtml,
        byte[]? adjunto,
        string? nombreAdjunto,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            sender = new
            {
                name = configuracion["Correo:NombreRemitente"] ?? "Centro Óptico San Francisco",
                email = remitente
            },
            to = new[] { new { email = destinatario } },
            subject = asunto,
            htmlContent = cuerpoHtml,
            attachment = adjunto is not null && !string.IsNullOrWhiteSpace(nombreAdjunto)
                ? new[] { new { name = nombreAdjunto, content = Convert.ToBase64String(adjunto) } }
                : null
        };

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        solicitud.Headers.Add("api-key", apiKey);
        solicitud.Headers.Accept.ParseAdd("application/json");
        solicitud.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }),
            Encoding.UTF8,
            "application/json");

        using var respuesta = await clienteHttpFactory.CreateClient().SendAsync(solicitud, cancellationToken);
        if (respuesta.IsSuccessStatusCode)
            return new ResultadoEnvioCorreo(true);

        logger.LogWarning("Brevo rechazó el envío de correo (HTTP {CodigoEstado}).", (int)respuesta.StatusCode);
        return new ResultadoEnvioCorreo(false, $"Brevo rechazó el envío (HTTP {(int)respuesta.StatusCode}).");
    }
}
