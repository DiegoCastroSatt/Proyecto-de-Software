using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Models;
using Microsoft.EntityFrameworkCore;
public class ReservaRepository : IReservaRepository
{
    private readonly OpticaDbContext _context;

    public ReservaRepository(OpticaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles()
    {
        return await _context.Horarios
            .Where(h => h.Estado == "Habilitada" && h.Fecha >= DateTime.Today)
            .Where(h => !_context.Reservas.Any(r => r.HorarioId == h.Id && r.Estado != "Cancelada"))
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.HoraInicio)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Reserva> CrearReserva(CrearReservaDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var horariosBloqueados = await _context.Horarios
            .FromSqlInterpolated($"SELECT id_horario, fecha, hora_inicio, hora_fin, estado FROM horarios_atencion WHERE id_horario = {dto.IdHorario} FOR UPDATE")
            .ToListAsync();
        var horario = horariosBloqueados.SingleOrDefault();

        if (horario is null || horario.Estado != "Habilitada" || horario.Fecha.Date < DateTime.Today)
        {
            throw new InvalidOperationException("El horario seleccionado ya no está disponible.");
        }

        if (await _context.Reservas.AnyAsync(r => r.HorarioId == horario.Id && r.Estado != "Cancelada"))
        {
            throw new InvalidOperationException("La hora seleccionada ya está reservada.");
        }

        var rut = dto.Rut.Trim().ToUpperInvariant();
        var cliente = await _context.Clientes.SingleOrDefaultAsync(c => c.Rut == rut);
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
            Estado = "Pendiente"
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