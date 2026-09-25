using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Services;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestReservas
{
    [Theory]
    [InlineData("12.345.678-5")]
    [InlineData("12.345.6785")]
    [InlineData("123456785")]
    public void NormalizarRut_QuitaSeparadoresYConservaDigitoVerificador(string rut)
    {
        Assert.Equal("123456785", RutChilenoValidator.Normalizar(rut));
    }

    [Theory]
    [InlineData("9 1234 5678", true)]
    [InlineData("+56 9 1234 5678", true)]
    [InlineData("91234567", false)]
    [InlineData("123456789", false)]
    public void TelefonoChilenoValidator_ValidaCantidadYFormato(string telefono, bool esperado)
    {
        Assert.Equal(esperado, TelefonoChilenoValidator.EsValido(telefono));
    }

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
            Rut = "12.345.678-5",
            Telefono = "9 1234 5678",
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

    [Fact]
    public async Task CrearReserva_RechazaRutConDigitoVerificadorInvalido()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = new ReservaService(repositorio);

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-9",
            IdHorario = 12
        }));

        Assert.Null(repositorio.UltimaSolicitud);
    }

    [Fact]
    public async Task CrearReserva_RechazaCorreoUsadoPorOtroClienteConMensajeClaro()
    {
        var repositorio = new ReservaRepositorioPrueba { CorreoUsadoPorOtroCliente = true };
        var servicio = new ReservaService(repositorio);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-5",
            Telefono = "9 1234 5678",
            Correo = "ana@example.com",
            IdHorario = 12
        }));

        Assert.Contains("correo ya está registrado", error.Message);
        Assert.Null(repositorio.UltimaSolicitud);
    }

    [Fact]
    public async Task CrearReserva_RechazaTelefonoConDigitosInsuficientes()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = new ReservaService(repositorio);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-5",
            Telefono = "91234567",
            IdHorario = 12
        }));

        Assert.Contains("celular válido de 9 dígitos", error.Message);
        Assert.Null(repositorio.UltimaSolicitud);
    }
}

internal sealed class ReservaRepositorioPrueba : IReservaRepository
{
    public CrearReservaDto? UltimaSolicitud { get; private set; }
    public Reserva? ReservaCreada { get; set; }
    public IReadOnlyList<Horario> Horarios { get; set; } = [];
    public bool CorreoUsadoPorOtroCliente { get; set; }

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles() => Task.FromResult(Horarios);

    public Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado) =>
        Task.FromResult(CorreoUsadoPorOtroCliente);

    public Task<Reserva> CrearReserva(CrearReservaDto dto)
    {
        UltimaSolicitud = dto;
        return Task.FromResult(ReservaCreada ?? new Reserva { Id = 1, HorarioId = dto.IdHorario });
    }

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora) => Task.FromResult(false);
    public Task<Reserva> Crear(Reserva reserva) => Task.FromResult(reserva);
    public Task<Reserva?> ObtenerPorId(int id) => Task.FromResult<Reserva?>(null);
}
