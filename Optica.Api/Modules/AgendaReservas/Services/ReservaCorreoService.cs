using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Interfaces;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed class ReservaCorreoService(IConfiguration configuracion, ILogger<ReservaCorreoService> logger, TimeProvider proveedorTiempo) : IReservaCorreoService
{
    private const string NombreCentro = "Centro Óptico San Francisco";

    public Task<bool> EnviarReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken)
    {
        var baseUrl = configuracion["Correo:UrlBaseApi"]?.TrimEnd('/');
        if (!EsUrlBaseSegura(baseUrl))
        {
            logger.LogWarning("No se envió el correo de reserva porque Correo:UrlBaseApi debe ser HTTPS (se permite HTTP solo en localhost).");
            return Task.FromResult(false);
        }

        var urlConfirmacion = $"{baseUrl}/api/Reservas/acciones/confirmar/{tokenConfirmacion}";
        var urlCancelacion = $"{baseUrl}/api/Reservas/acciones/cancelar/{tokenCancelacion}";
        var googleCalendarUrl = CrearUrlGoogleCalendar(reserva);
        var nombre = HtmlEncoder.Default.Encode(reserva.NombreCliente);
        var fecha = HtmlEncoder.Default.Encode(reserva.Fecha.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CL")));
        var inicio = HtmlEncoder.Default.Encode(reserva.HoraInicio.ToString(@"hh\:mm"));
        var fin = HtmlEncoder.Default.Encode(reserva.HoraFin.ToString(@"hh\:mm"));
        var html = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Hola, {nombre}</h1><p>Hemos registrado tu hora de atención:</p>
            <p style="padding:16px;background:#f3f8f6;border-radius:8px"><strong>Fecha:</strong> {fecha}<br>
            <strong>Hora:</strong> {inicio} - {fin}</p><p>Por favor, confirma tu asistencia.</p>
            <p><a href="{urlConfirmacion}" style="display:inline-block;padding:13px 20px;border-radius:7px;background:#176b6c;color:#fff;text-decoration:none;font-weight:bold">CONFIRMAR MI HORA</a></p>
            <p>¿No puedes asistir?</p><p><a href="{urlCancelacion}" style="display:inline-block;padding:13px 20px;border-radius:7px;background:#a83a32;color:#fff;text-decoration:none;font-weight:bold">CANCELAR MI HORA</a></p>
            <p><a href="{googleCalendarUrl}" style="color:#176b6c">Agregar a Google Calendar</a> · También adjuntamos un archivo compatible con calendarios.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;
        var adjunto = CrearArchivoCalendario(reserva);
        return Enviar(reserva.CorreoCliente, $"Reserva registrada - {NombreCentro}", html, adjunto, cancellationToken);
    }

    public Task<bool> EnviarRecordatorio(ReservaCorreoDto reserva, CancellationToken cancellationToken)
    {
        var nombre = HtmlEncoder.Default.Encode(reserva.NombreCliente);
        var fecha = HtmlEncoder.Default.Encode(reserva.Fecha.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CL")));
        var inicio = HtmlEncoder.Default.Encode(reserva.HoraInicio.ToString(@"hh\:mm"));
        var fin = HtmlEncoder.Default.Encode(reserva.HoraFin.ToString(@"hh\:mm"));
        var html = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="color:#176b6c;font-weight:bold">{NombreCentro}</p><h1 style="font-size:24px">Recordatorio de tu hora</h1>
            <p>Hola, {nombre}. Te recordamos tu atención:</p><p style="padding:16px;background:#f3f8f6;border-radius:8px">
            <strong>Fecha:</strong> {fecha}<br><strong>Hora:</strong> {inicio} - {fin}</p>
            <p>Te esperamos en {NombreCentro}.</p></td></tr></table></td></tr></table></body></html>
            """;
        return Enviar(reserva.CorreoCliente, $"Recordatorio de tu hora - {NombreCentro}", html, CrearArchivoCalendario(reserva), cancellationToken);
    }

    private async Task<bool> Enviar(string? destinatario, string asunto, string html, byte[] calendario, CancellationToken cancellationToken)
    {
        var host = configuracion["Correo:SmtpHost"];
        var remitente = configuracion["Correo:Remitente"];
        if (string.IsNullOrWhiteSpace(destinatario) || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(remitente))
        {
            logger.LogWarning("No se envió un correo de reservas: falta destinatario o configuración SMTP.");
            return false;
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(remitente, configuracion["Correo:NombreRemitente"] ?? NombreCentro),
                Subject = asunto,
                Body = html,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            mensaje.To.Add(destinatario);
            mensaje.Attachments.Add(new Attachment(new MemoryStream(calendario), "reserva.ics", "text/calendar"));

            using var smtp = new SmtpClient(host, int.TryParse(configuracion["Correo:Puerto"], out var puerto) ? puerto : 587)
            {
                EnableSsl = !bool.TryParse(configuracion["Correo:UsarTls"], out var usarTls) || usarTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };
            var usuario = configuracion["Correo:Usuario"];
            var clave = configuracion["Correo:Clave"];
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                smtp.Credentials = new NetworkCredential(usuario, clave ?? string.Empty);
            }

            await smtp.SendMailAsync(mensaje, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogError("Falló el envío SMTP de un correo de reservas.");
            return false;
        }
    }

    private string CrearUrlGoogleCalendar(ReservaCorreoDto reserva)
    {
        var (inicioUtc, finUtc) = ObtenerRangoUtc(reserva);
        var parametros = new Dictionary<string, string>
        {
            ["action"] = "TEMPLATE",
            ["text"] = $"Atención óptica - {NombreCentro}",
            ["dates"] = $"{inicioUtc:yyyyMMdd'T'HHmmss'Z'}/{finUtc:yyyyMMdd'T'HHmmss'Z'}",
            ["details"] = $"Atención reservada en {NombreCentro}.",
            ["location"] = NombreCentro,
            ["ctz"] = "America/Santiago"
        };
        return "https://calendar.google.com/calendar/render?" + string.Join("&", parametros.Select(par =>
            $"{Uri.EscapeDataString(par.Key)}={Uri.EscapeDataString(par.Value)}"));
    }

    private byte[] CrearArchivoCalendario(ReservaCorreoDto reserva)
    {
        var (inicioUtc, finUtc) = ObtenerRangoUtc(reserva);
        var contenido = string.Join("\r\n", [
            "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Optica San Francisco//Reservas//ES", "CALSCALE:GREGORIAN",
            "BEGIN:VEVENT", $"UID:reserva-{reserva.Id}@opticasanfrancisco", $"DTSTAMP:{proveedorTiempo.GetUtcNow():yyyyMMdd'T'HHmmss'Z'}",
            $"DTSTART:{inicioUtc:yyyyMMdd'T'HHmmss'Z'}", $"DTEND:{finUtc:yyyyMMdd'T'HHmmss'Z'}",
            $"SUMMARY:{EscaparIcs($"Atención óptica - {NombreCentro}")}", $"LOCATION:{EscaparIcs(NombreCentro)}",
            $"DESCRIPTION:{EscaparIcs($"Reserva de {reserva.NombreCliente} en {NombreCentro}")}", "END:VEVENT", "END:VCALENDAR", ""
        ]);
        return new UTF8Encoding(true).GetBytes(contenido);
    }

    private static (DateTime InicioUtc, DateTime FinUtc) ObtenerRangoUtc(ReservaCorreoDto reserva)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");
        var inicioLocal = DateTime.SpecifyKind(reserva.Fecha.Date.Add(reserva.HoraInicio), DateTimeKind.Unspecified);
        var finLocal = DateTime.SpecifyKind(reserva.Fecha.Date.Add(reserva.HoraFin), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(inicioLocal, zona), TimeZoneInfo.ConvertTimeToUtc(finLocal, zona));
    }

    private static string EscaparIcs(string valor) => valor.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r\n", "\\n").Replace("\n", "\\n");

    private static bool EsUrlBaseSegura(string? valor)
    {
        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps
            || (uri.Scheme == Uri.UriSchemeHttp && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)));
    }
}
