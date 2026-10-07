using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.AgendaReservas.Interfaces;
using Optica.Api.Modules.AgendaReservas.Services;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestReservas
{
    private static ReservaService CrearServicio(ReservaRepositorioPrueba repositorio) =>
        new(repositorio, new ReservaCorreoPrueba(), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance);

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
        var servicio = CrearServicio(repositorio);

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
        var servicio = CrearServicio(repositorio);

        var respuesta = await servicio.CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez",
            Rut = "12.345.678-5",
            Telefono = "9 1234 5678",
            Correo = "ana@example.com",
            IdHorario = 12
        });

        Assert.Equal(7, respuesta.Id);
        Assert.Equal(12, respuesta.IdHorario);
        Assert.Equal("Pendiente", respuesta.Estado);
    }

    [Fact]
    public async Task CrearReserva_EnviaCorreoInicialConAccionesYCalendario()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaCreada = new Reserva { Id = 18, HorarioId = 12, Fecha = DateTime.Today.AddDays(3), Hora = new TimeSpan(9, 0, 0), Estado = "Pendiente" },
            DatosCorreo = new ReservaCorreoDto { Id = 18, NombreCliente = "Ana Pérez", CorreoCliente = "ana@example.com", Fecha = DateTime.Today.AddDays(3), HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(9, 30, 0), Estado = "Pendiente" }
        };
        var correo = new ReservaCorreoPrueba();
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance);

        var respuesta = await servicio.CrearReserva(new CrearReservaDto { NombreCompleto = "Ana Pérez", Rut = "12.345.678-5", Telefono = "9 1234 5678", Correo = "ana@example.com", IdHorario = 12 });

        Assert.True(respuesta.CorreoEnviado);
        Assert.Equal("ana@example.com", correo.ReservaCreada?.CorreoCliente);
        Assert.Equal(43, correo.TokenConfirmacion?.Length);
        Assert.Equal(43, correo.TokenCancelacion?.Length);
        Assert.NotEqual(correo.TokenConfirmacion, correo.TokenCancelacion);
        Assert.Equal(64, repositorio.TokenConfirmacionHash?.Length);
        Assert.Equal(64, repositorio.TokenCancelacionHash?.Length);
        Assert.NotEqual(correo.TokenConfirmacion, repositorio.TokenConfirmacionHash);
    }

    [Fact]
    public async Task CrearReserva_NoPierdeLaReservaSiFallaElCorreo()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaCreada = new Reserva { Id = 19, HorarioId = 12, Estado = "Pendiente" },
            DatosCorreo = new ReservaCorreoDto { Id = 19, CorreoCliente = "ana@example.com", Fecha = DateTime.Today.AddDays(2) }
        };
        var correo = new ReservaCorreoPrueba { EnvioCorrecto = false };
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance);

        var respuesta = await servicio.CrearReserva(new CrearReservaDto { NombreCompleto = "Ana Pérez", Rut = "12.345.678-5", Telefono = "9 1234 5678", Correo = "ana@example.com", IdHorario = 12 });

        Assert.Equal(19, respuesta.Id);
        Assert.False(respuesta.CorreoEnviado);
        Assert.NotNull(repositorio.ReservaCreada);
    }

    [Fact]
    public async Task AccionPorToken_ConfirmaYEsIdempotente()
    {
        var repositorio = CrearRepositorioAccion("confirmar-seguro", "cancelar-descartado");
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.EjecutarAccionPorToken("confirmar-seguro", true, CancellationToken.None);
        var repetido = await servicio.EjecutarAccionPorToken("confirmar-seguro", true, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.Confirmada, resultado.Tipo);
        Assert.Equal(TipoResultadoAccionReserva.YaConfirmada, repetido.Tipo);
        Assert.Equal("Confirmada", repositorio.EstadoReserva);
    }

    [Fact]
    public async Task AccionPorToken_CancelaYLiberarHorario()
    {
        var repositorio = CrearRepositorioAccion("confirmar-descartado", "cancelar-seguro");
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.EjecutarAccionPorToken("cancelar-seguro", false, CancellationToken.None);
        var repetido = await servicio.EjecutarAccionPorToken("cancelar-seguro", false, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.Cancelada, resultado.Tipo);
        Assert.Equal(TipoResultadoAccionReserva.YaCancelada, repetido.Tipo);
        Assert.Equal("Cancelada", repositorio.EstadoReserva);
        Assert.Equal("Habilitada", repositorio.Horarios.Single(h => h.Id == 12).Estado);
    }

    [Fact]
    public async Task AccionPorToken_InvalidoNoModificaLaReserva()
    {
        var repositorio = CrearRepositorioAccion("token-correcto", "cancelar-correcto");

        var resultado = await CrearServicio(repositorio).EjecutarAccionPorToken("token-ajeno", true, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.TokenInvalido, resultado.Tipo);
        Assert.Equal("Pendiente", repositorio.EstadoReserva);
    }

    [Fact]
    public async Task TokenDeConfirmacionNoAutorizaLaCancelacion()
    {
        var repositorio = CrearRepositorioAccion("solo-confirmar", "solo-cancelar");

        var resultado = await CrearServicio(repositorio).EjecutarAccionPorToken("solo-confirmar", false, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.TokenInvalido, resultado.Tipo);
        Assert.Equal("Pendiente", repositorio.EstadoReserva);
    }

    [Fact]
    public async Task AccionPorToken_ReservaYaCanceladaNoVuelveACambiarEstado()
    {
        var repositorio = CrearRepositorioAccion("token-confirmar", "token-cancelar");
        repositorio.EstadoReserva = "Cancelada";

        var resultado = await CrearServicio(repositorio).EjecutarAccionPorToken("token-confirmar", true, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.YaCancelada, resultado.Tipo);
        Assert.Equal("Cancelada", repositorio.EstadoReserva);
    }

    [Fact]
    public async Task AccionPorToken_ExpiradoNoModificaLaReserva()
    {
        var repositorio = CrearRepositorioAccion("token-expirado", "token-cancelar");
        repositorio.TokenExpiraUtc = DateTime.UtcNow.AddMinutes(-1);

        var resultado = await CrearServicio(repositorio).EjecutarAccionPorToken("token-expirado", true, CancellationToken.None);

        Assert.Equal(TipoResultadoAccionReserva.TokenExpirado, resultado.Tipo);
        Assert.Equal("Pendiente", repositorio.EstadoReserva);
    }

    [Fact]
    public async Task Recordatorio_NoIncluyeCanceladasYNoseDuplica()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            Recordatorios =
            [
                new() { Id = 1, NombreCliente = "Cliente vigente", CorreoCliente = "vigente@example.com", Fecha = DateTime.Today.AddDays(1), HoraInicio = new TimeSpan(9, 0, 0), Estado = "Pendiente" },
                new() { Id = 2, NombreCliente = "Cliente cancelado", CorreoCliente = "cancelado@example.com", Fecha = DateTime.Today.AddDays(1), HoraInicio = new TimeSpan(10, 0, 0), Estado = "Cancelada" },
                new() { Id = 3, NombreCliente = "Cliente confirmado", CorreoCliente = "confirmado@example.com", Fecha = DateTime.Today.AddDays(1), HoraInicio = new TimeSpan(11, 0, 0), Estado = "Confirmada" }
            ]
        };
        var correo = new ReservaCorreoPrueba();
        var servicio = new RecordatorioReservasService(repositorio, correo, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordatorioReservasService>.Instance);

        await servicio.EnviarPendientes(CancellationToken.None);
        await servicio.EnviarPendientes(CancellationToken.None);

        Assert.Equal(2, correo.RecordatoriosEnviados.Count);
        Assert.Equal(1, correo.RecordatoriosEnviados[0].Id);
        Assert.Equal(new[] { 1, 3 }, correo.RecordatoriosEnviados.Select(r => r.Id).Order().ToArray());
        Assert.Equal(2, repositorio.RecordatoriosMarcados.Count);
    }

    private static ReservaRepositorioPrueba CrearRepositorioAccion(string tokenConfirmacion, string tokenCancelacion)
    {
        return new ReservaRepositorioPrueba
        {
            ReservaId = 25,
            ClienteId = 8,
            HorarioId = 12,
            EstadoReserva = "Pendiente",
            TokenConfirmacionHash = TokenAccionReserva.ObtenerHash(tokenConfirmacion),
            TokenCancelacionHash = TokenAccionReserva.ObtenerHash(tokenCancelacion),
            TokenExpiraUtc = DateTime.UtcNow.AddDays(2),
            Horarios = [new() { Id = 12, Estado = "Inhabilitada", Fecha = DateTime.Today.AddDays(1), HoraInicio = new TimeSpan(9, 0, 0) }]
        };
    }

    [Fact]
    public async Task ObtenerHorariosDisponibles_DelegaEnElRepositorio()
    {
        var horarios = new List<Horario>
        {
            new() { Id = 1, Fecha = new DateTime(2026, 9, 22), Estado = "Habilitada" }
        };
        var repositorio = new ReservaRepositorioPrueba { Horarios = horarios };
        var servicio = CrearServicio(repositorio);

        var resultado = await servicio.ObtenerHorariosDisponibles();

        Assert.Single(resultado);
        Assert.Equal(1, resultado[0].Id);
    }

    [Fact]
    public async Task ReprogramarReserva_ActualizaHorarioSinCambiarCliente()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaId = 25, ClienteId = 8, HorarioId = 15, Horarios =
        [ new() { Id = 15, Estado = "Inhabilitada" }, new() { Id = 27, Estado = "Habilitada" } ] };
        var servicio = CrearServicio(repositorio);

        await servicio.ReprogramarReserva(25, 27, CancellationToken.None);

        Assert.Equal(27, repositorio.HorarioId);
        Assert.Equal(8, repositorio.ClienteId);
        Assert.Equal(1, repositorio.CantidadReservas);
        Assert.Equal("Habilitada", repositorio.Horarios.Single(h => h.Id == 15).Estado);
        Assert.Equal("Inhabilitada", repositorio.Horarios.Single(h => h.Id == 27).Estado);
    }

    [Fact]
    public async Task ReprogramarReserva_RechazaHorarioOcupadoSinModificarReserva()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaId = 25, ClienteId = 8, HorarioId = 15, HorarioOcupado = true,
            Horarios = [ new() { Id = 15, Estado = "Inhabilitada" }, new() { Id = 27, Estado = "Habilitada" } ] };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).ReprogramarReserva(25, 27, CancellationToken.None));

        Assert.Contains("ya no está disponible", error.Message);
        Assert.Equal(15, repositorio.HorarioId);
        Assert.Equal(8, repositorio.ClienteId);
    }

    [Fact]
    public async Task ReprogramarReserva_RechazaHorarioInexistenteSinModificarReserva()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaId = 25, ClienteId = 8, HorarioId = 15 };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CrearServicio(repositorio).ReprogramarReserva(25, 27, CancellationToken.None));

        Assert.Equal(15, repositorio.HorarioId);
    }

    [Fact]
    public async Task ReprogramarReserva_RechazaReservaInexistente()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CrearServicio(new ReservaRepositorioPrueba()).ReprogramarReserva(25, 27, CancellationToken.None));
    }

    [Fact]
    public async Task ReprogramarReserva_RechazaHorarioInhabilitadoSinModificarReserva()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaId = 25, ClienteId = 8, HorarioId = 15,
            Horarios = [ new() { Id = 27, Estado = "Inhabilitada" } ] };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).ReprogramarReserva(25, 27, CancellationToken.None));

        Assert.Contains("no está disponible", error.Message);
        Assert.Equal(15, repositorio.HorarioId);
    }

    [Fact]
    public async Task ReprogramarReserva_RechazaElMismoHorarioActual()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaId = 25, ClienteId = 8, HorarioId = 15,
            Horarios = [ new() { Id = 15, Estado = "Inhabilitada" } ] };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).ReprogramarReserva(25, 15, CancellationToken.None));

        Assert.Contains("distinto al horario actual", error.Message);
        Assert.Equal(15, repositorio.HorarioId);
        Assert.Equal(8, repositorio.ClienteId);
    }

    [Fact]
    public async Task CrearReserva_RechazaHorarioInvalido()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = CrearServicio(repositorio);

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
        var servicio = CrearServicio(repositorio);

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
        var servicio = CrearServicio(repositorio);

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
        var servicio = CrearServicio(repositorio);

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

    [Fact]
    public async Task CrearReserva_RechazaCorreoAusente()
    {
        var repositorio = new ReservaRepositorioPrueba();
        var error = await Assert.ThrowsAsync<ArgumentException>(() => CrearServicio(repositorio).CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez", Rut = "12.345.678-5", Telefono = "9 1234 5678", IdHorario = 12
        }));

        Assert.Contains("correo electrónico es obligatorio", error.Message);
        Assert.Null(repositorio.UltimaSolicitud);
    }
}

internal sealed class ReservaRepositorioPrueba : IReservaRepository
{
    public CrearReservaDto? UltimaSolicitud { get; private set; }
    public Reserva? ReservaCreada { get; set; }
    public ReservaCorreoDto? DatosCorreo { get; set; }
    public IReadOnlyList<Horario> Horarios { get; set; } = [];
    public IReadOnlyList<ReservaCorreoDto> Recordatorios { get; set; } = [];
    public HashSet<int> RecordatoriosMarcados { get; } = [];
    public bool CorreoUsadoPorOtroCliente { get; set; }
    public int ReservaId { get; set; }
    public int ClienteId { get; set; }
    public int HorarioId { get; set; }
    public bool HorarioOcupado { get; set; }
    public int CantidadReservas { get; private set; } = 1;
    public string EstadoReserva { get; set; } = "Pendiente";
    public string? TokenConfirmacionHash { get; set; }
    public string? TokenCancelacionHash { get; set; }
    public DateTime TokenExpiraUtc { get; set; } = DateTime.UtcNow.AddDays(2);

    public Task<IReadOnlyList<Horario>> ObtenerHorariosDisponibles(DateTime? fecha = null, int? excluirReservaId = null) => Task.FromResult(Horarios);

    public Task<IReadOnlyList<ReservaAgendaResponseDto>> ObtenerAgenda(bool historialAtendidas, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReservaAgendaResponseDto>>([]);

    public Task<bool> ExisteCorreoEnOtroCliente(string correo, string rutNormalizado) =>
        Task.FromResult(CorreoUsadoPorOtroCliente);

    public Task<Reserva> CrearReserva(CrearReservaDto dto, string tokenConfirmacionHash, string tokenCancelacionHash)
    {
        UltimaSolicitud = dto;
        TokenConfirmacionHash = tokenConfirmacionHash;
        TokenCancelacionHash = tokenCancelacionHash;
        if (ReservaCreada is null) ReservaCreada = new Reserva { Id = 1, HorarioId = dto.IdHorario, Estado = "Pendiente" };
        return Task.FromResult(ReservaCreada);
    }

    public Task<ReservaCorreoDto?> ObtenerDatosCorreoReserva(int id, CancellationToken cancellationToken) => Task.FromResult(DatosCorreo);

    public Task<ResultadoAccionReserva> EjecutarAccionPorToken(string tokenHash, bool confirmar, DateTime ahoraUtc, CancellationToken cancellationToken)
    {
        var esperado = confirmar ? TokenConfirmacionHash : TokenCancelacionHash;
        if (esperado is null || esperado != tokenHash) return Task.FromResult(new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenInvalido });
        if (confirmar && EstadoReserva == "Confirmada") return Task.FromResult(new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.YaConfirmada });
        if (EstadoReserva == "Cancelada") return Task.FromResult(new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.YaCancelada });
        if (TokenExpiraUtc <= ahoraUtc) return Task.FromResult(new ResultadoAccionReserva { Tipo = TipoResultadoAccionReserva.TokenExpirado });
        if (confirmar) EstadoReserva = "Confirmada";
        else
        {
            EstadoReserva = "Cancelada";
            var horario = Horarios.SingleOrDefault(h => h.Id == HorarioId);
            if (horario is not null) horario.Estado = "Habilitada";
        }
        return Task.FromResult(new ResultadoAccionReserva { Tipo = confirmar ? TipoResultadoAccionReserva.Confirmada : TipoResultadoAccionReserva.Cancelada });
    }

    public Task<IReadOnlyList<ReservaCorreoDto>> ObtenerReservasParaRecordatorio(DateTimeOffset ahoraChile, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReservaCorreoDto>>(Recordatorios.Where(r => r.Estado is "Pendiente" or "Confirmada" && !RecordatoriosMarcados.Contains(r.Id)).ToList());

    public Task<bool> MarcarRecordatorioEnviado(int id, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        if (RecordatoriosMarcados.Contains(id) || !Recordatorios.Any(r => r.Id == id && r.Estado is "Pendiente" or "Confirmada")) return Task.FromResult(false);
        RecordatoriosMarcados.Add(id);
        return Task.FromResult(true);
    }

    public Task DesmarcarRecordatorio(int id, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        RecordatoriosMarcados.Remove(id);
        return Task.CompletedTask;
    }

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora) => Task.FromResult(false);
    public Task<Reserva> Crear(Reserva reserva) => Task.FromResult(reserva);
    public Task<Reserva?> ObtenerPorId(int id) => Task.FromResult<Reserva?>(null);
    public Task<bool> CancelarReserva(int id, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> ReprogramarReserva(int id, int idHorario, CancellationToken cancellationToken)
    {
        if (ReservaId != id) return Task.FromResult(false);
        var horario = Horarios.SingleOrDefault(h => h.Id == idHorario);
        if (horario is null) throw new KeyNotFoundException("El horario seleccionado no existe.");
        if (idHorario == HorarioId) throw new InvalidOperationException("El nuevo horario debe ser distinto al horario actual.");
        if (horario.Estado != "Habilitada" || HorarioOcupado)
            throw new InvalidOperationException(horario.Estado != "Habilitada" ? "El horario seleccionado no está disponible." : "El horario seleccionado ya no está disponible.");
        var anterior = Horarios.Single(h => h.Id == HorarioId);
        anterior.Estado = "Habilitada";
        horario.Estado = "Inhabilitada";
        HorarioId = idHorario;
        return Task.FromResult(true);
    }
}

internal sealed class ReservaCorreoPrueba : IReservaCorreoService
{
    public ReservaCorreoDto? ReservaCreada { get; private set; }
    public string? TokenConfirmacion { get; private set; }
    public string? TokenCancelacion { get; private set; }
    public List<ReservaCorreoDto> RecordatoriosEnviados { get; } = [];
    public bool EnvioCorrecto { get; set; } = true;

    public Task<bool> EnviarReservaCreada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken)
    {
        ReservaCreada = reserva;
        TokenConfirmacion = tokenConfirmacion;
        TokenCancelacion = tokenCancelacion;
        return Task.FromResult(EnvioCorrecto);
    }

    public Task<bool> EnviarRecordatorio(ReservaCorreoDto reserva, CancellationToken cancellationToken)
    {
        RecordatoriosEnviados.Add(reserva);
        return Task.FromResult(EnvioCorrecto);
    }
}
