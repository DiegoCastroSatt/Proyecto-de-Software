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

    public async Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles()
    {
        var ahoraChile = HoraChile.ObtenerAhora(_proveedorTiempo);
        return await _context.Horarios
            .Where(h => h.Estado == "Habilitada"
                && (h.Fecha.Date > ahoraChile.Date
                    || (h.Fecha.Date == ahoraChile.Date && h.HoraInicio > ahoraChile.TimeOfDay)))
            .Where(h => !_context.Reservas.Any(r => r.HorarioId == h.Id && r.Estado != "Cancelada"))
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.HoraInicio)
            .AsNoTracking()
            .ToListAsync();
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

    public async Task<Reserva> CrearReserva(CrearReservaDto dto)
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

        if (await _context.Reservas.AnyAsync(r => r.HorarioId == horario.Id && r.Estado != "Cancelada"))
        {
            throw new InvalidOperationException("La hora seleccionada ya está reservada.");
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
            Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? null : dto.Motivo.Trim()
        };
        horario.Estado = "Inhabilitada";
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return reserva;
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
}
