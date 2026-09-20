using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.AgendaReservas.Models;

namespace Optica.Api.Modules.AgendaReservas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HorariosController(OpticaDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HorarioAdministracionResponseDto>>> Listar(
        [FromQuery] string? fecha,
        CancellationToken cancellationToken)
    {
        var consulta = context.Horarios
            .AsNoTracking()
            .Where(h => h.Fecha >= DateTime.Today);

        if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParseExact(
                fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaFiltro))
        {
            consulta = consulta.Where(h => h.Fecha.Date == fechaFiltro.Date);
        }

        var horarios = await consulta
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.HoraInicio)
            .ToListAsync(cancellationToken);

        return Ok(horarios.Select(Mapear));
    }

    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<HorarioAdministracionResponseDto>>> Crear(
        CrearHorariosDto dto,
        CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(dto.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            || !TimeSpan.TryParse(dto.HoraInicio, CultureInfo.InvariantCulture, out var horaInicio)
            || !TimeSpan.TryParse(dto.HoraFin, CultureInfo.InvariantCulture, out var horaFin))
        {
            return BadRequest(new { mensaje = "La fecha y las horas no tienen un formato válido." });
        }

        if (fecha.Date < DateTime.Today || horaInicio >= horaFin)
        {
            return BadRequest(new { mensaje = "La fecha o el rango de horas no son válidos." });
        }

        if (dto.DuracionMinutos <= 0 || dto.DuracionMinutos > 480)
        {
            return BadRequest(new { mensaje = "La duración debe estar entre 1 y 480 minutos." });
        }

        var administradorId = dto.IdAdministrador ?? await context.Administradores
            .Where(a => a.Estado == "Activo")
            .Select(a => (int?)a.IdAdministrador)
            .FirstOrDefaultAsync(cancellationToken);

        if (administradorId is null)
        {
            return BadRequest(new { mensaje = "No existe un administrador activo para asignar el horario." });
        }

        var duracion = TimeSpan.FromMinutes(dto.DuracionMinutos);
        var existentes = await context.Horarios
            .Where(h => h.Fecha.Date == fecha.Date && h.HoraInicio >= horaInicio && h.HoraInicio < horaFin)
            .Select(h => h.HoraInicio)
            .ToListAsync(cancellationToken);
        var existentesSet = existentes.ToHashSet();
        var nuevos = new List<Horario>();

        for (var inicio = horaInicio; inicio + duracion <= horaFin; inicio += duracion)
        {
            if (existentesSet.Contains(inicio))
            {
                continue;
            }

            nuevos.Add(new Horario
            {
                AdministradorId = administradorId.Value,
                Fecha = fecha.Date,
                HoraInicio = inicio,
                HoraFin = inicio + duracion,
                Estado = "Habilitada"
            });
        }

        if (nuevos.Count == 0)
        {
            return Conflict(new { mensaje = "Todos los bloques de ese rango ya existen." });
        }

        context.Horarios.AddRange(nuevos);
        await context.SaveChangesAsync(cancellationToken);
        return Ok(nuevos.Select(Mapear));
    }

    [HttpPatch("{idHorario:int}/estado")]
    public async Task<ActionResult<HorarioAdministracionResponseDto>> CambiarEstado(
        int idHorario,
        CambiarEstadoHorarioDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.Estado is not ("Habilitada" or "Inhabilitada"))
        {
            return BadRequest(new { mensaje = "El estado debe ser Habilitada o Inhabilitada." });
        }

        var horario = await context.Horarios.FirstOrDefaultAsync(h => h.Id == idHorario, cancellationToken);
        if (horario is null)
        {
            return NotFound(new { mensaje = "No se encontró el horario." });
        }

        horario.Estado = dto.Estado;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(Mapear(horario));
    }

    private static HorarioAdministracionResponseDto Mapear(Horario horario) => new()
    {
        IdHorario = horario.Id,
        Fecha = horario.Fecha,
        HoraInicio = horario.HoraInicio.ToString(@"hh\:mm"),
        HoraFin = horario.HoraFin.ToString(@"hh\:mm"),
        Estado = horario.Estado
    };
}
