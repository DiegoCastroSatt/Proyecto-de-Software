using Optica.Api.Modules.Notificaciones.Interfaces;
using Optica.Api.Modules.Pedidos.Interfaces;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Optica.Api.Modules.Pedidos.Services;

public sealed class PedidoCorreoService(IEmailService emailService) : IPedidoCorreoService
{
    private const string NombreCentro = "Centro Óptico San Francisco";

    public async Task<bool> EnviarPedidoListoAsync(string? destinatario, string nombreCliente, int idPedido, CancellationToken cancellationToken)
    {
        var nombre = HtmlEncoder.Default.Encode(nombreCliente);
        var numeroPedido = HtmlEncoder.Default.Encode(idPedido.ToString(CultureInfo.InvariantCulture));
        var html = $"""
            <!doctype html><html lang="es"><body style="margin:0;background:#f3f7f6;font-family:Arial,sans-serif;color:#18333c">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px"><tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;background:#fff;border-radius:14px;padding:32px">
            <tr><td><p style="margin:0 0 8px;color:#176b6c;font-weight:bold">{NombreCentro}</p>
            <h1 style="font-size:24px">¡Tu pedido está listo para retirar!</h1>
            <p>Hola, {nombre}. Ya puedes retirar tu pedido en nuestro centro óptico.</p>
            <p style="padding:16px;background:#f3f8f6;border-radius:8px"><strong>Pedido:</strong> #{numeroPedido}</p>
            <p>Te esperamos en {NombreCentro}.</p>
            <p style="color:#63777c">Si tienes dudas, puedes contactarnos directamente en el centro.</p>
            </td></tr></table></td></tr></table></body></html>
            """;
        return await emailService.EnviarAsync(destinatario, "Pedido listo para retirar - Centro Óptico San Francisco", html,
            cancellationToken, importante: true);
    }
}
