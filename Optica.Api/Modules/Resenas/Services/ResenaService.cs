using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.Resenas.DTOs;
using Optica.Api.Modules.Resenas.Interfaces;
using Optica.Api.Modules.Resenas.Models;

namespace Optica.Api.Modules.Resenas.Services;

public sealed class ResenaService(IResenaRepository repositorio, TimeProvider reloj) : IResenaService
{
    public Task<IReadOnlyList<ResenaResponseDto>> ListarPorProducto(int idProducto, CancellationToken cancellationToken) =>
        repositorio.ListarPorProducto(idProducto, cancellationToken);

    public Task<ResumenResenasDto> ObtenerResumen(int idProducto, CancellationToken cancellationToken) =>
        repositorio.ObtenerResumen(idProducto, cancellationToken);

    public Task<IReadOnlyList<ProductoResenableDto>> ListarProductosComprados(int idCliente, CancellationToken cancellationToken) =>
        repositorio.ListarProductosComprados(idCliente, cancellationToken);

    public async Task<ResenaResponseDto> Crear(int idCliente, int idProducto, CrearResenaDto dto, CancellationToken cancellationToken)
    {
        if (dto.Calificacion is < 1 or > 5)
            throw new ArgumentException("La calificación debe estar entre 1 y 5.");
        var comentario = dto.Comentario?.Trim();
        if (string.IsNullOrWhiteSpace(comentario) || comentario.Length < 3 || comentario.Length > 1000)
            throw new ArgumentException("El comentario debe tener entre 3 y 1000 caracteres.");
        if (!await repositorio.ExisteProducto(idProducto, cancellationToken))
            throw new KeyNotFoundException("No se encuentra el producto.");
        if (!await repositorio.ClienteComproProducto(idCliente, idProducto, cancellationToken))
            throw new InvalidOperationException("Solo puedes reseñar productos incluidos en una compra registrada a tu nombre.");
        if (await repositorio.YaExisteResena(idCliente, idProducto, cancellationToken))
            throw new ResenaDuplicadaException();

        var fecha = reloj.GetUtcNow().UtcDateTime;
        var resena = new Resena { IdCliente = idCliente, IdProducto = idProducto, Calificacion = dto.Calificacion,
            Comentario = comentario, FechaCreacionUtc = fecha };
        try
        {
            await repositorio.Agregar(resena, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ResenaDuplicadaException();
        }
        return new ResenaResponseDto(resena.IdResena, idProducto, resena.Calificacion, resena.Comentario,
            string.Empty, resena.FechaCreacionUtc, null);
    }
}

public sealed class ResenaDuplicadaException : Exception
{
    public ResenaDuplicadaException() : base("Ya publicaste una reseña para este producto.") { }
}
