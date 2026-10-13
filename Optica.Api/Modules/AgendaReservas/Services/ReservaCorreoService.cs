using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Interfaces;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.AgendaReservas.Services;

public sealed class ReservaCorreoService(IConfiguration configuracion, ILogger<ReservaCorreoService> logger, TimeProvider proveedorTiempo, IEmailService emailService) : IReservaCorreoService
{
    private const string NombreCentro = "Centro Óptico San Francisco";

    public Task<bool> EnviarReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken)
        => EnviarCorreoReserva(reserva, tokenConfirmacion, tokenCancelacion, reprogramada: false, cancellationToken);

    public Task<bool> EnviarReservaReprogramada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken)
        => EnviarCorreoReserva(reserva, tokenConfirmacion, tokenCancelacion, reprogramada: true, cancellationToken);

    private Task<bool> EnviarCorreoReserva(
        ReservaCorreoDto reserva,
        string tokenConfirmacion,
        string tokenCancelacion,
        bool reprogramada,
        CancellationToken cancellationToken)
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
        var html = CrearHtmlReserva(reserva, urlConfirmacion, urlCancelacion, googleCalendarUrl, reprogramada);
        var asunto = reprogramada ? $"Reserva reprogramada - {NombreCentro}" : $"Reserva registrada - {NombreCentro}";
        var adjunto = CrearArchivoCalendario(reserva);
        return Enviar(reserva.CorreoCliente, asunto, html, adjunto, cancellationToken);
    }

    internal static string CrearHtmlReserva(
        ReservaCorreoDto reserva,
        string urlConfirmacion,
        string urlCancelacion,
        string googleCalendarUrl,
        bool reprogramada)
    {
        var nombre = HtmlEncoder.Default.Encode(reserva.NombreCliente);
        var fecha = HtmlEncoder.Default.Encode(reserva.Fecha.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CL")));
        var inicio = HtmlEncoder.Default.Encode(reserva.HoraInicio.ToString(@"hh\:mm"));
        var fin = HtmlEncoder.Default.Encode(reserva.HoraFin.ToString(@"hh\:mm"));
        var numeroReserva = HtmlEncoder.Default.Encode(reserva.Id.ToString(CultureInfo.InvariantCulture));
        var mensajeReserva = reprogramada
            ? "Tu reserva fue reprogramada. Confirma la nueva fecha y hora para mantenerla vigente."
            : "Hemos registrado tu hora de atención. Por favor, confirma tu asistencia.";
        return $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Hola, {nombre}</h1><p>{HtmlEncoder.Default.Encode(mensajeReserva)}</p>
            <p><strong>Reserva:</strong> #{numeroReserva}</p>
            <p style="padding:16px;background:#f3f8f6;border-radius:8px"><strong>Fecha:</strong> {fecha}<br>
            <strong>Hora:</strong> {inicio} - {fin}</p>
            <p>{(reprogramada ? "Confirma la nueva hora desde este correo." : "Por favor, confirma tu asistencia.")}</p>
            <p><a href="{urlConfirmacion}" style="display:inline-block;padding:13px 20px;border-radius:7px;background:#176b6c;color:#fff;text-decoration:none;font-weight:bold">CONFIRMAR MI HORA</a></p>
            <p>¿No puedes asistir?</p><p><a href="{urlCancelacion}" style="display:inline-block;padding:13px 20px;border-radius:7px;background:#a83a32;color:#fff;text-decoration:none;font-weight:bold">CANCELAR MI HORA</a></p>
            <p><a href="{googleCalendarUrl}" style="color:#176b6c">Agregar a Google Calendar</a> · También adjuntamos un archivo compatible con calendarios.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;
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
        return await emailService.EnviarAsync(destinatario, asunto, html, cancellationToken,
            adjunto: calendario, nombreAdjunto: "reserva.ics", tipoAdjunto: "text/calendar");
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
