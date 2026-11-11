using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.DataProtection;
using Optica.Api.Modules.AgendaReservas.Models;

namespace Optica.Api.Modules.Encuestas.Services;

/// <summary>Prepara el correo que invita al cliente a responder la encuesta de su atención.</summary>
public sealed class EncuestaCorreoService(IConfiguration configuracion, IDataProtectionProvider protectionProvider)
{
    public const string TipoCorreo = "Encuesta";
    private const string NombreCentro = "Centro Óptico San Francisco";
    // Mismo propósito que los demás correos de reservas: los descifra CorreoReservaBackgroundService.
    private const string PropositoProteccion = "Optica.CorreosReservas.v1";

    public CorreoReservaPendiente PrepararSolicitud(
        int reservaId,
        int horarioId,
        string nombreCliente,
        string correoCliente,
        DateTime fechaAtencion,
        TimeSpan horaInicio,
        string token,
        DateTime envioUtc,
        DateTime creadoUtc)
    {
        var baseFrontend = (configuracion["Correo:UrlBaseFrontend"] ?? "http://localhost:4200").TrimEnd('/');
        var enlace = $"{baseFrontend}/encuesta?token={Uri.EscapeDataString(token)}";
        var html = CrearHtml(nombreCliente, fechaAtencion, horaInicio, enlace);
        return new CorreoReservaPendiente
        {
            ReservaId = reservaId,
            HorarioId = horarioId,
            Tipo = TipoCorreo,
            Destinatario = correoCliente,
            Asunto = $"¿Cómo fue tu atención? - {NombreCentro}",
            CuerpoHtmlProtegido = protectionProvider.CreateProtector(PropositoProteccion).Protect(html),
            CreadoUtc = creadoUtc,
            ProximoIntentoUtc = envioUtc
        };
    }

    internal static string CrearHtml(string nombreCliente, DateTime fechaAtencion, TimeSpan horaInicio, string enlace)
    {
        var nombre = HtmlEncoder.Default.Encode(nombreCliente);
        var fecha = HtmlEncoder.Default.Encode(fechaAtencion.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CL")));
        var hora = HtmlEncoder.Default.Encode(horaInicio.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
        var enlaceSeguro = HtmlEncoder.Default.Encode(enlace);
        return $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">Hola, {nombre}</h1>
            <p>Gracias por visitarnos. Queremos saber cómo fue tu atención del <strong>{fecha}</strong> a las <strong>{hora}</strong>.</p>
            <p>Son solo tres preguntas y tu respuesta es anónima.</p>
            <p><a href="{enlaceSeguro}" style="display:inline-block;padding:13px 20px;border-radius:7px;background:#176b6c;color:#fff;text-decoration:none;font-weight:bold">RESPONDER ENCUESTA</a></p>
            <p style="color:#63777c;font-size:13px">El enlace se puede usar una sola vez y vence en 14 días.</p>
            <p style="color:#63777c">{NombreCentro}</p></td></tr></table></td></tr></table></body></html>
            """;
    }
}
