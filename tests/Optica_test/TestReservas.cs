using Optica.Api.Modules.AgendaReservas.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestReservas
{
    [Fact]
    public async Task CrearReserva_RechazaDatosObligatoriosAusentes()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = new ReservaService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "",
            Rut = "",
            IdHorario = 0
        }));

        Assert.Null(repositorio.UltimaSolicitud);
    }

    [Fact]
    public async Task CrearReserva_DevuelveRespuestaMapeada()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaCreada = new Reserva
            {
                Id = 7,
                HorarioId = 12,
                Fecha = new DateTime(2026, 9, 22),
                Hora = new TimeSpan(9, 0, 0),
                Estado = "Pendiente"
            }
        };
        var servicio = new ReservaService(repositorio);

        var respuesta = await servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-9",
            IdHorario = 12
        });

        Assert.Equal(7, respuesta.Id);
        Assert.Equal(12, respuesta.IdHorario);
        Assert.Equal("Pendiente", respuesta.Estado);
    }

    [Fact]
    public async Task ObtenerHorariosDisponibles_DelegaEnElRepositorio()
    {
        var horarios = new List<Horario>
        {
            new() { Id = 1, Fecha = new DateTime(2026, 9, 22), Estado = "Habilitada" }
        };
        var repositorio = new ReservaRepositorioPrueba { Horarios = horarios };
        var servicio = new ReservaService(repositorio);

        var resultado = await servicio.ObtenerHorariosDisponibles();

        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
    }

    [Fact]
    public async Task CrearReserva_RechazaHorarioInvalido()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = new ReservaService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-9",
            IdHorario = -1
        }));
    }
}

internal sealed class ReservaRepositorioPrueba : IReservaRepository
{
    public CrearReservaDto? UltimaSolicitud { get; private set; }
    public Reserva? ReservaCreada { get; set; }
    public IReadOnlyList<Horario> Horarios { get; set; } = [];

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() => Task.FromResult(Horarios);

    public Task<Reserva> CrearReserva(CrearReservaDto dto)
    {
        UltimaSolicitud = dto;
        return Task.FromResult(ReservaCreada ?? new Reserva { Id = 1, HorarioId = dto.IdHorario });
    }

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora) => Task.FromResult(false);
    public Task<Reserva> Crear(Reserva reserva) => Task.FromResult(reserva);
    public Task<Reserva?> ObtenerPorId(int id) => Task.FromResult<Reserva?>(null);
}
