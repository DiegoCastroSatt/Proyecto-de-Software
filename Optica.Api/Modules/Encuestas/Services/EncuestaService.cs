using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Encuestas.DTOs;
using Optica.Api.Modules.Encuestas.Interfaces;
using Optica.Api.Modules.Encuestas.Models;

namespace Optica.Api.Modules.Encuestas.Services;

public sealed class EncuestaService(OpticaDbContext db, TimeProvider proveedorTiempo) : IEncuestaService
{
    public const int LargoMaximoComentario = 1000;
    public const int TamanoPaginaMaximo = 50;
    private const int LargoMaximoToken = 200;

    public async Task<(ResultadoEncuesta Resultado, EncuestaPublicaDto? Encuesta)> ObtenerPorToken(
        string token, CancellationToken cancellationToken)
    {
        var encuesta = await BuscarPendientePorToken(token, cancellationToken);
        if (encuesta is null) return (ResultadoEncuesta.NoEncontrada, null);
        if (EstaVencida(encuesta)) return (ResultadoEncuesta.Vencida, null);

        var horario = await (
            from reserva in db.Reservas
            join h in db.Horarios on reserva.HorarioId equals h.Id
            where reserva.Id == encuesta.ReservaId
            select new { h.Fecha, h.HoraInicio }).SingleOrDefaultAsync(cancellationToken);
        if (horario is null) return (ResultadoEncuesta.NoEncontrada, null);

        // Solo se exponen la fecha y la hora: el enlace no revela datos del cliente.
        return (ResultadoEncuesta.Disponible, new EncuestaPublicaDto(
            horario.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            horario.HoraInicio.ToString(@"hh\:mm", CultureInfo.InvariantCulture)));
    }

    public async Task<ResultadoEncuesta> Responder(string token, ResponderEncuestaDto dto, CancellationToken cancellationToken)
    {
        var comentario = Validar(dto);

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
        var encuesta = await BuscarPendientePorToken(token, cancellationToken);
        if (encuesta is null) return ResultadoEncuesta.NoEncontrada;
        if (EstaVencida(encuesta)) return ResultadoEncuesta.Vencida;

        // Anular el token es lo que impide una segunda respuesta. Como el hash es token de concurrencia,
        // si otro envío lo anuló primero, este guardado falla y no se inserta una segunda respuesta.
        encuesta.TokenHash = null;
        encuesta.TokenExpiraUtc = null;
        encuesta.Respondida = true;
        db.RespuestasEncuestaAtencion.Add(new RespuestaEncuestaAtencion
        {
            CalificacionGeneral = dto.CalificacionGeneral,
            CalificacionTrato = dto.CalificacionTrato,
            CalificacionPuntualidad = dto.CalificacionPuntualidad,
            Comentario = comentario,
            FechaRespuesta = HoraChile.ObtenerAhora(proveedorTiempo).Date
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return ResultadoEncuesta.YaRespondida;
        }

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoEncuesta.Respondida;
    }

    public async Task<ResumenEncuestasDto> ObtenerResumen(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var (desdeUtc, hastaUtc) = RangoUtc(desde, hasta);
        var ahoraUtc = proveedorTiempo.GetUtcNow().UtcDateTime;

        // La tasa se calcula sobre las encuestas cuyo envío ya ocurrió dentro del periodo.
        var encuestas = await db.EncuestasAtencion.AsNoTracking()
            .Where(e => e.FechaEnvioUtc >= desdeUtc && e.FechaEnvioUtc < hastaUtc && e.FechaEnvioUtc <= ahoraUtc)
            .Select(e => e.Respondida)
            .ToListAsync(cancellationToken);
        var enviadas = encuestas.Count;
        var respondidasDeEnviadas = encuestas.Count(respondida => respondida);

        var respuestas = await ConsultarRespuestas(desde, hasta).ToListAsync(cancellationToken);
        var distribucion = Enumerable.Range(1, 5).Reverse()
            .Select(estrellas => new DistribucionEstrellasDto(
                estrellas, respuestas.Count(r => r.CalificacionGeneral == estrellas)))
            .ToList();
        var evolucion = respuestas
            .GroupBy(r => new { r.FechaRespuesta.Year, r.FechaRespuesta.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new EvolucionMensualEncuestasDto(
                $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                g.Count(),
                Redondear(g.Average(r => r.CalificacionGeneral)),
                Redondear(g.Average(r => r.CalificacionTrato)),
                Redondear(g.Average(r => r.CalificacionPuntualidad))))
            .ToList();

        return new ResumenEncuestasDto(
            enviadas,
            respuestas.Count,
            enviadas == 0 ? 0 : Math.Round(respondidasDeEnviadas * 100.0 / enviadas, 1),
            Promedio(respuestas, r => r.CalificacionGeneral),
            Promedio(respuestas, r => r.CalificacionTrato),
            Promedio(respuestas, r => r.CalificacionPuntualidad),
            distribucion,
            evolucion);
    }

    public async Task<ComentariosEncuestasPaginadosDto> ObtenerComentarios(
        DateTime desde,
        DateTime hasta,
        int? calificacion,
        bool soloConComentario,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken)
    {
        if (calificacion is < 1 or > 5) throw new ArgumentException("La calificación debe estar entre 1 y 5.");
        if (pagina < 1) throw new ArgumentException("La página debe ser mayor o igual a 1.");
        if (tamanoPagina is < 1 or > TamanoPaginaMaximo)
            throw new ArgumentException($"El tamaño de página debe estar entre 1 y {TamanoPaginaMaximo}.");

        var consulta = ConsultarRespuestas(desde, hasta);
        if (calificacion is not null) consulta = consulta.Where(r => r.CalificacionGeneral == calificacion);
        if (soloConComentario) consulta = consulta.Where(r => r.Comentario != null && r.Comentario != "");

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .OrderByDescending(r => r.FechaRespuesta).ThenByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync(cancellationToken);

        return new ComentariosEncuestasPaginadosDto(
            elementos.Select(r => new ComentarioEncuestaDto(
                r.Id,
                r.FechaRespuesta.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                r.CalificacionGeneral,
                r.CalificacionTrato,
                r.CalificacionPuntualidad,
                r.ComentarioOculto ? null : r.Comentario,
                r.ComentarioOculto)).ToList(),
            total,
            pagina,
            tamanoPagina);
    }

    public async Task<bool> CambiarVisibilidadComentario(int idRespuesta, bool oculto, CancellationToken cancellationToken)
    {
        var respuesta = await db.RespuestasEncuestaAtencion.SingleOrDefaultAsync(r => r.Id == idRespuesta, cancellationToken);
        if (respuesta is null) return false;
        if (respuesta.ComentarioOculto == oculto) return true;
        respuesta.ComentarioOculto = oculto;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Valida la respuesta y devuelve el comentario normalizado (null si está vacío).</summary>
    internal static string? Validar(ResponderEncuestaDto dto)
    {
        if (!EsCalificacionValida(dto.CalificacionGeneral)
            || !EsCalificacionValida(dto.CalificacionTrato)
            || !EsCalificacionValida(dto.CalificacionPuntualidad))
            throw new ArgumentException("Cada pregunta debe calificarse con 1 a 5 estrellas.");

        var comentario = dto.Comentario?.Trim();
        if (comentario?.Length > LargoMaximoComentario)
            throw new ArgumentException($"El comentario puede tener hasta {LargoMaximoComentario} caracteres.");
        return string.IsNullOrEmpty(comentario) ? null : comentario;
    }

    private async Task<EncuestaAtencion?> BuscarPendientePorToken(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > LargoMaximoToken) return null;
        var hash = TokenAccionReserva.ObtenerHash(token.Trim());
        return await db.EncuestasAtencion.SingleOrDefaultAsync(e => e.TokenHash == hash && !e.Respondida, cancellationToken);
    }

    private bool EstaVencida(EncuestaAtencion encuesta) =>
        encuesta.TokenExpiraUtc is null || encuesta.TokenExpiraUtc <= proveedorTiempo.GetUtcNow().UtcDateTime;

    private IQueryable<RespuestaEncuestaAtencion> ConsultarRespuestas(DateTime desde, DateTime hasta)
    {
        var inicio = desde.Date;
        var fin = hasta.Date;
        return db.RespuestasEncuestaAtencion.AsNoTracking()
            .Where(r => r.FechaRespuesta >= inicio && r.FechaRespuesta <= fin);
    }

    /// <summary>Convierte un rango de días de Chile (ambos incluidos) a instantes UTC.</summary>
    private static (DateTime DesdeUtc, DateTime HastaUtc) RangoUtc(DateTime desde, DateTime hasta) =>
        (HoraChile.ConvertirAUtc(desde.Date, TimeSpan.Zero), HoraChile.ConvertirAUtc(hasta.Date.AddDays(1), TimeSpan.Zero));

    private static bool EsCalificacionValida(int calificacion) => calificacion is >= 1 and <= 5;

    private static double? Promedio(List<RespuestaEncuestaAtencion> respuestas, Func<RespuestaEncuestaAtencion, int> selector) =>
        respuestas.Count == 0 ? null : Redondear(respuestas.Average(selector));

    private static double Redondear(double valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
