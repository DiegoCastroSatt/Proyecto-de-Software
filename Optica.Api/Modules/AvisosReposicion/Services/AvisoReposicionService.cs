using System.Text.Encodings.Web;
using Optica.Api.Modules.AvisosReposicion.DTOs;
using Optica.Api.Modules.AvisosReposicion.Interfaces;
using Optica.Api.Modules.AvisosReposicion.Models;
using Optica.Api.Modules.Notificaciones.Interfaces;

namespace Optica.Api.Modules.AvisosReposicion.Services;

public sealed class AvisoReposicionService(
    IAvisoReposicionRepository repositorio,
    IEmailService emailService,
    TimeProvider reloj,
    ILogger<AvisoReposicionService> logger) : IAvisoReposicionService
{
    public async Task<AvisoReposicionResponseDto> Crear(
        int idCliente,
        int idProducto,
        CancellationToken cancellationToken)
    {
        if (idProducto <= 0)
            throw new ArgumentException("El producto no es válido.");

        var cliente = await repositorio.ObtenerCliente(idCliente, cancellationToken)
            ?? throw new KeyNotFoundException("No se encuentra el cliente.");
        var producto = await repositorio.ObtenerProducto(idProducto, cancellationToken)
            ?? throw new KeyNotFoundException("No se encuentra el producto.");
        if (producto.Stock > 0)
            throw new InvalidOperationException("Solo puedes solicitar un aviso para un producto agotado.");
        if (await repositorio.ExisteAvisoAbierto(cliente.Rut, idProducto, cancellationToken))
            throw new AvisoReposicionDuplicadoException();

        var aviso = await repositorio.Crear(new AvisoReposicion
        {
            Rut = cliente.Rut,
            IdProducto = producto.IdProducto,
            Fecha = reloj.GetUtcNow().UtcDateTime,
            Estado = "Abierta"
        }, cancellationToken);

        return new AvisoReposicionResponseDto(
            aviso.IdAviso,
            aviso.IdProducto,
            producto.Nombre,
            new DateTimeOffset(DateTime.SpecifyKind(aviso.Fecha, DateTimeKind.Utc)),
            aviso.Estado);
    }

    public async Task<IReadOnlyList<AvisoReposicionResponseDto>> ListarPorCliente(
        int idCliente,
        CancellationToken cancellationToken) =>
        await repositorio.ListarPorCliente(idCliente, cancellationToken);

    public async Task<IReadOnlyList<AvisoReposicionAdministracionResponseDto>> ListarTodas(
        CancellationToken cancellationToken) =>
        await repositorio.ListarTodas(cancellationToken);

    public async Task ProcesarReposicionesDisponibles(CancellationToken cancellationToken)
    {
        var avisos = await repositorio.ListarPendientesConStock(cancellationToken);
        foreach (var aviso in avisos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var nombreProducto = HtmlEncoder.Default.Encode(aviso.NombreProducto);
                var resultado = await emailService.EnviarConResultadoAsync(
                    aviso.CorreoCliente,
                    $"Producto disponible nuevamente: {aviso.NombreProducto}",
                    $"<p>Hola {HtmlEncoder.Default.Encode(aviso.NombreCliente)},</p>" +
                    $"<p>El producto <strong>{nombreProducto}</strong> ya está disponible nuevamente.</p>",
                    cancellationToken);

                if (!resultado.Exitoso)
                {
                    logger.LogWarning(
                        "No se pudo enviar el aviso de reposición {AvisoId}; se reintentará en el siguiente ciclo.",
                        aviso.IdAviso);
                    continue;
                }

                await repositorio.Cerrar(aviso.IdAviso, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    "Falló el procesamiento del aviso de reposición {AvisoId} ({TipoError}); se reintentará en el siguiente ciclo.",
                    aviso.IdAviso,
                    ex.GetType().Name);
            }
        }
    }

}

public sealed class AvisoReposicionDuplicadoException()
    : Exception("Ya tienes una solicitud abierta para este producto.");
