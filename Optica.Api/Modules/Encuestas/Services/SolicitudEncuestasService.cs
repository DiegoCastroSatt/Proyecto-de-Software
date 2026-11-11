using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Encuestas.Interfaces;
using Optica.Api.Modules.Encuestas.Models;

namespace Optica.Api.Modules.Encuestas.Services;

/// <summary>
/// Crea automáticamente una encuesta por cada atención realizada.
/// Mientras no se registre la asistencia, se asume que toda reserva "Realizada" fue atendida
/// (ver MEJORAS-asistencia-reservas.md).
/// </summary>
public sealed class SolicitudEncuestasService(
    OpticaDbContext db,
    EncuestaCorreoService correoService,
    TimeProvider proveedorTiempo,
    ILogger<SolicitudEncuestasService> logger) : ISolicitudEncuestasService
{
    /// <summary>Tiempo entre el fin de la atención y el envío del correo.</summary>
    public static readonly TimeSpan EsperaTrasAtencion = TimeSpan.FromHours(2);

    /// <summary>Solo se consideran atenciones terminadas en este periodo, para no encuestar el historial.</summary>
    public static readonly TimeSpan VentanaAtenciones = TimeSpan.FromDays(3);

    /// <summary>Vigencia del enlace contada desde el envío del correo.</summary>
    public static readonly TimeSpan VigenciaEnlace = TimeSpan.FromDays(14);

    public async Task<int> ProgramarPendientes(CancellationToken cancellationToken)
    {
        var ahoraUtc = proveedorTiempo.GetUtcNow().UtcDateTime;
        // Margen de un día extra para cubrir el cambio de zona horaria; el filtro exacto se hace en memoria.
        var fechaMinima = HoraChile.ObtenerAhora(proveedorTiempo).Date.AddDays(-(VentanaAtenciones.Days + 1));

        var candidatas = await (
            from reserva in db.Reservas
            join horario in db.Horarios on reserva.HorarioId equals horario.Id
            join cliente in db.Clientes on reserva.ClienteId equals cliente.IdCliente
            where reserva.Estado == "Realizada"
                && horario.Fecha >= fechaMinima
                && cliente.Correo != null && cliente.Correo != ""
                && !db.EncuestasAtencion.Any(e => e.ReservaId == reserva.Id)
            select new
            {
                ReservaId = reserva.Id,
                reserva.HorarioId,
                horario.Fecha,
                horario.HoraInicio,
                horario.HoraFin,
                cliente.Nombre,
                Correo = cliente.Correo!
            }).ToListAsync(cancellationToken);

        var creadas = 0;
        foreach (var atencion in candidatas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finUtc = HoraChile.ConvertirAUtc(atencion.Fecha, atencion.HoraFin);
            if (finUtc > ahoraUtc || finUtc < ahoraUtc - VentanaAtenciones) continue;

            var envioUtc = finUtc + EsperaTrasAtencion;
            if (envioUtc < ahoraUtc) envioUtc = ahoraUtc;
            var token = TokenAccionReserva.Crear();

            // La encuesta y su correo se guardan juntos: o quedan ambos o ninguno.
            db.EncuestasAtencion.Add(new EncuestaAtencion
            {
                ReservaId = atencion.ReservaId,
                TokenHash = TokenAccionReserva.ObtenerHash(token),
                TokenExpiraUtc = envioUtc + VigenciaEnlace,
                FechaEnvioUtc = envioUtc
            });
            db.CorreosReserva.Add(correoService.PrepararSolicitud(
                atencion.ReservaId,
                atencion.HorarioId,
                atencion.Nombre,
                atencion.Correo,
                atencion.Fecha,
                atencion.HoraInicio,
                token,
                envioUtc,
                ahoraUtc));

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                creadas++;
            }
            catch (DbUpdateException)
            {
                // Otra ejecución ya creó la encuesta de esta reserva (índice único por reserva).
                db.ChangeTracker.Clear();
                logger.LogWarning("No se creó la encuesta de la reserva {ReservaId} porque ya existía.", atencion.ReservaId);
            }
        }

        if (creadas > 0) logger.LogInformation("Se programaron {Cantidad} encuestas de atención.", creadas);
        return creadas;
    }
}
