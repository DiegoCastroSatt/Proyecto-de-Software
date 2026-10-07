using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.AgendaReservas.Services;
using Microsoft.EntityFrameworkCore;
public class ReservaRepository : IReservaRepository
{
    private readonly OpticaDbContext _context;
    private readonly TimeProvider _proveedorTiempo;

    public ReservaRepository(OpticaDbContext context, TimeProvider proveedorTiempo)
    {
        _context = context;
        _proveedorTiempo = proveedorTiempo;
    }

    public async Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null)
    {
        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        return await _context.Horarios
            .Where(h => h.Estado == "Habilitada"
                && (h.Fecha.Date > ahoraChile.Date
                    || (h.Fecha.Date == ahoraChile.Date && h.HoraInicio > ahoraChile.TimeOfDay)))
            .Where(h => fecha == null || h.Fecha.Date == fecha.Value.Date)
            .Where(h => excluirReservaId == null || !_context.Reservas.Any(r => r.Id == excluirReservaId.Value
                && r.HorarioId == h.Id && r.Estado != "Cancelada"))
            .Where(h => !_context.Reservas.Any(r => r.HorarioId == h.Id && r.Estado != "Cancelada"
                && (excluirReservaId == null || r.Id != excluirReservaId.Value)))
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.HoraInicio)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var horarioAnteriorId = await _context.Reservas
            .Where(r => r.Id == id)
            .Select(r => (int?)r.HorarioId)
            .SingleOrDefaultAsync(cancellationToken);
        if (horarioAnteriorId is null)
        {
            return false;
        }

        // Se usa el mismo orden de bloqueo que la cancelación: horario y luego reserva.
        await _context.Horarios
            .FromSqlInterpolated($"SELECT id_horario, id_administrador, fecha, hora_inicio, hora_fin, estado FROM horarios_atencion WHERE id_horario = {horarioAnteriorId.Value} OR id_horario = {idHorario} ORDER BY id_horario FOR UPDATE")
            .ToListAsync(cancellationToken);
        await _context.Reservas
            .FromSqlInterpolated($"SELECT id_reserva, id_cliente, id_horario, motivo, estado FROM reservas WHERE id_reserva = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        var reserva = await _context.Reservas.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (reserva is null || reserva.HorarioId != horarioAnteriorId.Value)
        {
            throw new InvalidOperationException("La reserva cambió mientras se procesaba. Actualiza la agenda e inténtalo nuevamente.");
        }

        if (reserva.Estado is "Cancelada" or "Realizada")
        {
            throw new InvalidOperationException("No se puede reprogramar una reserva cancelada o realizada.");
        }

        var horarioNuevo = await _context.Horarios.SingleOrDefaultAsync(h => h.Id == idHorario, cancellationToken);
        if (horarioNuevo is null)
        {
            throw new KeyNotFoundException("El horario seleccionado no existe.");
        }

        if (horarioNuevo.Id == reserva.HorarioId)
        {
            throw new InvalidOperationException("El nuevo horario debe ser distinto al horario actual.");
        }

        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        if (horarioNuevo.Estado != "Habilitada"
            || horarioNuevo.Fecha.Date < ahoraChile.Date
            || (horarioNuevo.Fecha.Date == ahoraChile.Date && horarioNuevo.HoraInicio <= ahoraChile.TimeOfDay)
            || await _context.Reservas.AnyAsync(r => r.Id != id && r.HorarioId == idHorario && r.Estado != "Cancelada", cancellationToken))
        {
            throw new InvalidOperationException(horarioNuevo.Estado != "Habilitada"
                ? "El horario seleccionado no está disponible."
                : "El horario seleccionado ya no está disponible.");
        }

        var horarioAnterior = await _context.Horarios.SingleOrDefaultAsync(h => h.Id == reserva.HorarioId, cancellationToken);
        if (horarioAnterior is null)
        {
            throw new InvalidOperationException("No se encontró el horario actual de la reserva.");
        }

        horarioAnterior.Estado = "Habilitada";
        horarioNuevo.Estado = "Inhabilitada";
        reserva.HorarioId = horarioNuevo.Id;
        reserva.TokenAccionExpiraUtc = ObtenerExpiracionAccionUtc(horarioNuevo.Fecha, horarioNuevo.HoraInicio);
        reserva.RecordatorioEnviadoUtc = null;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(
        bool historialAtendidas,
        CancellationToken cancellationToken)
    {
        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        var fechaHoy = ahoraChile.Date;
        var horaActual = ahoraChile.TimeOfDay;
        var consulta =
            from reserva in _context.Reservas.AsNoTracking()
            join horario in _context.Horarios.AsNoTracking() on reserva.HorarioId equals horario.Id
            join cliente in _context.Clientes.AsNoTracking() on reserva.ClienteId equals cliente.IdCliente
            select new { reserva, horario, cliente };

        consulta = historialAtendidas
            ? consulta.Where(item => item.reserva.Estado == "Realizada"
                || (item.reserva.Estado != "Cancelada"
                    && (item.horario.Fecha.Date < fechaHoy
                        || (item.horario.Fecha.Date == fechaHoy && item.horario.HoraInicio <= horaActual))))
            : consulta.Where(item => item.reserva.Estado != "Realizada"
                && item.reserva.Estado != "Cancelada"
                && (item.horario.Fecha.Date > fechaHoy
                    || (item.horario.Fecha.Date == fechaHoy && item.horario.HoraInicio > horaActual)));

        var ordenada = historialAtendidas
            ? consulta.OrderByDescending(item => item.horario.Fecha).ThenByDescending(item => item.horario.HoraInicio)
            : consulta.OrderBy(item => item.horario.Fecha).ThenBy(item => item.horario.HoraInicio);

        return await ordenada
            .Select(item => new ReservaAgendaResponseDto
            {
                Id = item.reserva.Id,
                Fecha = item.horario.Fecha,
                HoraInicio = item.horario.HoraInicio,
                HoraFin = item.horario.HoraFin,
                Estado = item.reserva.Estado != "Realizada"
                    && (item.horario.Fecha.Date < fechaHoy
                        || (item.horario.Fecha.Date == fechaHoy && item.horario.HoraInicio <= horaActual))
                    ? "Realizada"
                    : item.reserva.Estado,
                Motivo = item.reserva.Motivo,
                NombreCliente = item.cliente.Nombre + " " + item.cliente.Apellido,
                RutCliente = item.cliente.Rut,
                TelefonoCliente = item.cliente.Telefono,
                CorreoCliente = item.cliente.Correo
            })
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado)
    {
        var correoNormalizado = correo.Trim().ToUpperInvariant();

        return _context.Clientes.AnyAsync(cliente =>
            cliente.Correo != null
            && cliente.Correo.Trim().ToUpper() == correoNormalizado
            && cliente.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() != rutNormalizado);
    }

    public async Task<Reserva> CrearReserva(CrearReservaDto dto, string tokenConfirmacionHash, string tokenCancelacionHash)
    {
        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var horariosBloqueados = await _context.Horarios
            .FromSqlInterpolated($"SELECT id_horario, id_administrador, fecha, hora_inicio, hora_fin, estado FROM horarios_atencion WHERE id_horario = {dto.IdHorario} FOR UPDATE")
            .ToListAsync();
        var horario = horariosBloqueados.SingleOrDefault();

        if (horario is null
            || horario.Estado != "Habilitada"
            || horario.Fecha.Date < ahoraChile.Date
            || (horario.Fecha.Date == ahoraChile.Date && horario.HoraInicio <= ahoraChile.TimeOfDay))
        {
            throw new InvalidOperationException("El horario seleccionado ya no está disponible.");
        }

        var rut = RutChilenoValidator.Normalizar(dto.Rut);
        var cliente = await _context.Clientes
            .Where(c => c.Rut.Replace(".", "").Replace("-", "").Trim().ToUpper() == rut)
            .OrderBy(c => c.IdCliente)
            .FirstOrDefaultAsync();
        if (cliente is null)
        {
            var nombrePartes = dto.NombreCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            cliente = new Cliente
            {
                Rut = rut,
                Nombre = nombrePartes.FirstOrDefault() ?? string.Empty,
                Apellido = nombrePartes.Length > 1 ? string.Join(' ', nombrePartes.Skip(1)) : "Sin apellido",
                Telefono = dto.Telefono.Trim(),
                Correo = dto.Correo.Trim()
            };
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
        }

        var reserva = new Reserva
        {
            ClienteId = cliente.IdCliente,
            HorarioId = horario.Id,
            Fecha = horario.Fecha,
            Hora = horario.HoraInicio,
            Estado = "Pendiente",
            Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim(),
            TokenConfirmacionHash = tokenConfirmacionHash,
            TokenCancelacionHash = tokenCancelacionHash,
            TokenAccionExpiraUtc = ObtenerExpiracionAccionUtc(horario.Fecha, horario.HoraInicio)
        };
        horario.Estado = "Inhabilitada";
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return reserva;
    }

    public async Task<ReservaCorreoDto?> ObtenerDatosCorreoReserva(int id, CancellationToken cancellationToken)
    {
        return await (
            from reserva in _context.Reservas.AsNoTracking()
            join horario in _context.Horarios.AsNoTracking() on reserva.HorarioId equals horario.Id
            join cliente in _context.Clientes.AsNoTracking() on reserva.ClienteId equals cliente.IdCliente
            where reserva.Id == id
            select new ReservaCorreoDto
            {
                Id = reserva.Id,
                NombreCliente = cliente.Nombre + " " + cliente.Apellido,
                CorreoCliente = cliente.Correo,
                Fecha = horario.Fecha,
                HoraInicio = horario.HoraInicio,
                HoraFin = horario.HoraFin,
                Estado = reserva.Estado
            }).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultadoAccionReserva> EjecutarAccionPorToken(
        string tokenHash,
        bool confirmar,
        DateTime ahoraUtc,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var reservaClave = await _context.Reservas.AsNoTracking()
            .Where(r => confirmar ? r.TokenConfirmacionHash == tokenHash : r.TokenCancelacionHash == tokenHash)
            .Select(r => new { r.Id, r.HorarioId })
            .SingleOrDefaultAsync(cancellationToken);
        if (reservaClave is null)
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido };
        }

        var horariosBloqueados = await _context.Horarios
            .FromSqlInterpolated($"SELECT id_horario, id_administrador, fecha, hora_inicio, hora_fin, estado FROM horarios_atencion WHERE id_horario = {reservaClave.HorarioId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        var horario = horariosBloqueados.SingleOrDefault();
        await _context.Reservas
            .FromSqlInterpolated($"SELECT id_reserva, id_cliente, id_horario, motivo, estado, token_confirmacion_hash, token_cancelacion_hash, token_accion_expira_utc, recordatorio_enviado_utc FROM reservas WHERE id_reserva = {reservaClave.Id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        var reserva = await _context.Reservas.SingleOrDefaultAsync(r => r.Id == reservaClave.Id, cancellationToken);
        if (reserva is null || horario is null)
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido };
        }
        if (reserva.HorarioId != reservaClave.HorarioId)
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.NoVigente };
        }

        var tokenGuardado = confirmar ? reserva.TokenConfirmacionHash : reserva.TokenCancelacionHash;
        if (!string.Equals(tokenGuardado, tokenHash, StringComparison.Ordinal))
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido };
        }

        var detalle = new ResultadoAccionReserva { Fecha = horario.Fecha, HoraInicio = horario.HoraInicio };
        if (confirmar && reserva.Estado == "Confirmada")
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.YaConfirmada, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }
        if (!confirmar && reserva.Estado == "Cancelada")
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.YaCancelada, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }
        if (reserva.Estado == "Cancelada")
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.YaCancelada, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }
        if (reserva.Estado == "Realizada")
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.NoVigente, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }
        if (reserva.TokenAccionExpiraUtc is null || reserva.TokenAccionExpiraUtc <= ahoraUtc)
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenExpirado, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }

        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        if (horario.Fecha.Date < ahoraChile.Date
            || (horario.Fecha.Date == ahoraChile.Date && horario.HoraInicio <= ahoraChile.TimeOfDay))
        {
            return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.NoVigente, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
        }

        if (confirmar)
        {
            if (reserva.Estado != "Pendiente")
            {
                return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.NoVigente, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
            }
            reserva.Estado = "Confirmada";
        }
        else
        {
            if (reserva.Estado is not ("Pendiente" or "Confirmada"))
            {
                return new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.NoVigente, Fecha = detalle.Fecha, HoraInicio = detalle.HoraInicio };
            }
            reserva.Estado = "Cancelada";
            horario.Estado = "Habilitada";
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ResultadoAccionReserva
        {
            Tipo = confirmar ? TipoResultadoAccionReserva.Confirmada : TipoResultadoAccionReserva.Cancelada,
            Fecha = detalle.Fecha,
            HoraInicio = detalle.HoraInicio
        };
    }

    public async Task<IReadOnlyList<ReservaCorreoDto>> ObtenerReservasParaRecordatorio(DateTimeOffset ahoraChile, CancellationToken cancellationToken)
    {
        var ahoraLocal = DateTime.SpecifyKind(ahoraChile.DateTime, DateTimeKind.Unspecified);
        var limiteLocal = ahoraLocal.AddHours(24);
        return await (
            from reserva in _context.Reservas.AsNoTracking()
            join horario in _context.Horarios.AsNoTracking() on reserva.HorarioId equals horario.Id
            join cliente in _context.Clientes.AsNoTracking() on reserva.ClienteId equals cliente.IdCliente
            where (reserva.Estado == "Pendiente" || reserva.Estado == "Confirmada")
                && reserva.RecordatorioEnviadoUtc == null
                && (horario.Fecha.Date > ahoraLocal.Date || (horario.Fecha.Date == ahoraLocal.Date && horario.HoraInicio > ahoraLocal.TimeOfDay))
                && (horario.Fecha.Date < limiteLocal.Date || (horario.Fecha.Date == limiteLocal.Date && horario.HoraInicio <= limiteLocal.TimeOfDay))
            orderby horario.Fecha, horario.HoraInicio
            select new ReservaCorreoDto
            {
                Id = reserva.Id,
                NombreCliente = cliente.Nombre + " " + cliente.Apellido,
                CorreoCliente = cliente.Correo,
                Fecha = horario.Fecha,
                HoraInicio = horario.HoraInicio,
                HoraFin = horario.HoraFin,
                Estado = reserva.Estado
            }).ToListAsync(cancellationToken);
    }

    public async Task<bool> MarcarRecordatorioEnviado(int id, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        var afectados = await _context.Reservas
            .Where(r => r.Id == id && (r.Estado == "Pendiente" || r.Estado == "Confirmada") && r.RecordatorioEnviadoUtc == null)
            .ExecuteUpdateAsync(actualizacion => actualizacion.SetProperty(r => r.RecordatorioEnviadoUtc, enviadoUtc), cancellationToken);
        return afectados == 1;
    }

    public async Task DesmarcarRecordatorio(int id, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        await _context.Reservas
            .Where(r => r.Id == id && r.RecordatorioEnviadoUtc == enviadoUtc && (r.Estado == "Pendiente" || r.Estado == "Confirmada"))
            .ExecuteUpdateAsync(actualizacion => actualizacion.SetProperty(r => r.RecordatorioEnviadoUtc, (DateTime?)null), cancellationToken);
    }

    private static DateTime ObtenerExpiracionAccionUtc(DateTime fecha, TimeSpan hora)
    {
        var horaLocal = DateTime.SpecifyKind(fecha.Date.Add(hora).AddDays(1), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(horaLocal, TimeZoneInfo.FindSystemTimeZoneById("America/Santiago"));
    }

    public async Task<bool> ExisteReserva(
        DateTime fecha,
        TimeSpan hora)
    {
        return await _context.Reservas
            .Join(_context.Horarios,
                reserva => reserva.HorarioId,
                horario => horario.Id,
                (reserva, horario) => new { reserva, horario })
            .AnyAsync(item =>
                item.horario.Fecha == fecha.Date &&
                item.horario.HoraInicio == hora &&
                item.reserva.Estado != "Cancelada");
    }

    public async Task<Reserva> Crear(Reserva reserva)
    {
        _context.Reservas.Add(reserva);

        await _context.SaveChangesAsync();

        return reserva;
    }

    public async Task<Reserva?> ObtenerPorId(int id)
    {
        return await _context.Reservas
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> CancelarReserva(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var horarioId = await _context.Reservas
            .Where(r => r.Id == id)
            .Select(r => (int?)r.HorarioId)
            .SingleOrDefaultAsync(cancellationToken);

        if (horarioId is null)
        {
            return false;
        }

        // El bloqueo del horario serializa la cancelación con una nueva reserva.
        var horariosBloqueados = await _context.Horarios
            .FromSqlInterpolated($"SELECT id_horario, id_administrador, fecha, hora_inicio, hora_fin, estado FROM horarios_atencion WHERE id_horario = {horarioId.Value} FOR UPDATE")
            .ToListAsync(cancellationToken);
        var horario = horariosBloqueados.SingleOrDefault();
        var reserva = await _context.Reservas.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (horario is null || reserva is null)
        {
            return false;
        }

        if (reserva.Estado == "Cancelada")
        {
            throw new InvalidOperationException("Esta reserva ya fue cancelada.");
        }

        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        if (reserva.Estado == "Realizada"
            || horario.Fecha.Date < ahoraChile.Date
            || (horario.Fecha.Date == ahoraChile.Date && horario.HoraInicio <= ahoraChile.TimeOfDay))
        {
            throw new InvalidOperationException("No se pueden cancelar horas que ya fueron realizadas.");
        }

        reserva.Estado = "Cancelada";
        horario.Estado = "Habilitada";
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
