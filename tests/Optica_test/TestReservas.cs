using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.DTOs;
using Optica.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.AgendaReservas.Interfaces;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestReservas
{
    private static ReservaService CrearServicio(ReservaRepositorioPrueba repositorio) =>
        new(repositorio, new ReservaCorreoPrueba(), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance, new ColaCorreosReprogramacion());

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
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance, new ColaCorreosReprogramacion());

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
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance, new ColaCorreosReprogramacion());

        var respuesta = await servicio.CrearReserva(new CrearReservaDto { NombreCompleto = "Ana Pérez", Rut = "12.345.678-5", Telefono = "9 1234 5678", Correo = "ana@example.com", IdHorario = 12 });

        Assert.Equal(19, respuesta.Id);
        Assert.False(respuesta.CorreoEnviado);
        Assert.NotNull(repositorio.ReservaCreada);
    }

    [Fact]
    public void PlantillaCorreoReprogramada_ContieneDatosActualesYAcciones()
    {
        var html = ReservaCorreoService.CrearHtmlReserva(
            new ReservaCorreoDto
            {
                Id = 25,
                NombreCliente = "Ana Pérez",
                Fecha = new DateTime(2026, 11, 16),
                HoraInicio = new TimeSpan(11, 0, 0),
                HoraFin = new TimeSpan(11, 30, 0)
            },
            "https://optica.example/api/Reservas/acciones/confirmar/token-confirmacion",
            "https://optica.example/api/Reservas/acciones/cancelar/token-cancelacion",
            "https://calendar.example/evento",
            reprogramada: true);

        var contenido = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Ana Pérez", contenido);
        Assert.Contains("Reserva:</strong> #25", contenido);
        var fechaNueva = new DateTime(2026, 11, 16).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CL"));
        var fechaAnterior = new DateTime(2026, 11, 14).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-CL"));
        Assert.Contains(fechaNueva, contenido);
        Assert.Contains("11:00 - 11:30", contenido);
        Assert.Contains("Tu reserva fue reprogramada", contenido);
        Assert.Contains("CONFIRMAR MI HORA", contenido);
        Assert.Contains("CANCELAR MI HORA", contenido);
        Assert.DoesNotContain(fechaAnterior, contenido);
    }

    [Fact]
    public void ModeloReserva_MapeaColumnaRecordatorioExistente()
    {
        var opciones = new DbContextOptionsBuilder<OpticaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new OpticaDbContext(opciones);
        var propiedad = contexto.Model.FindEntityType(typeof(Reserva))!
            .FindProperty(nameof(Reserva.RecordatorioEnviadoUtc));

        Assert.NotNull(propiedad);
        Assert.Equal("recordatorio_enviado_utc", propiedad!.GetColumnName());
    }

    [Fact]
    public void AbrirEnlaceDeCancelacion_NoCancelaHastaLaConfirmacionExplicita()
    {
        var repositorio = CrearRepositorioAccion("confirmar-seguro", "cancelar-seguro");
        var controlador = new ReservasController(CrearServicio(repositorio))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var pagina = Assert.IsType<ContentResult>(controlador.CancelarDesdeCorreo("cancelar-seguro"));

        Assert.Contains("method=\"post\"", pagina.Content);
        Assert.Equal("Pendiente", repositorio.EstadoReserva);
        Assert.Equal("Inhabilitada", repositorio.Horarios.Single(h => h.Id == 12).Estado);
    }

    [Fact]
    public async Task ConfirmarDesdeCorreo_ActualizaLaReservaYDevuelveResultado()
    {
        var repositorio = CrearRepositorioAccion("confirmar-seguro", "cancelar-seguro");
        var controlador = new ReservasController(CrearServicio(repositorio))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var respuesta = Assert.IsType<ContentResult>(await controlador.EjecutarConfirmacionDesdeCorreo("confirmar-seguro", CancellationToken.None));

        Assert.Equal("Confirmada", repositorio.EstadoReserva);
        Assert.Contains("Tu hora quedó confirmada correctamente", System.Net.WebUtility.HtmlDecode(respuesta.Content));
    }

    [Fact]
    public async Task CancelarDesdeCorreo_ActualizaLaReservaYLiberarHorario()
    {
        var repositorio = CrearRepositorioAccion("confirmar-seguro", "cancelar-seguro");
        var controlador = new ReservasController(CrearServicio(repositorio))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var respuesta = Assert.IsType<ContentResult>(await controlador.EjecutarCancelacionDesdeCorreo("cancelar-seguro", CancellationToken.None));

        Assert.Equal("Cancelada", repositorio.EstadoReserva);
        Assert.Equal("Habilitada", repositorio.Horarios.Single(h => h.Id == 12).Estado);
        Assert.Contains("hora quedó liberada", System.Net.WebUtility.HtmlDecode(respuesta.Content));
    }

    [Fact]
    public async Task CorreoReserva_ConservaLosBotonesConUrlLocalYPublica()
    {
        var clienteCorreo = new ServicioEmailPrueba();
        var configuracion = new Microsoft.Extensions.Configuration.ConfigurationManager
        {
            ["Correo:SmtpHost"] = "smtp.example.test",
            ["Correo:Remitente"] = "optica@example.test",
            ["Correo:UrlBaseApi"] = "http://localhost:8080"
        };
        var servicio = new ReservaCorreoService(configuracion,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaCorreoService>.Instance,
            TimeProvider.System,
            clienteCorreo);
        var reserva = new ReservaCorreoDto
        {
            Id = 31,
            CorreoCliente = "cliente@example.test",
            NombreCliente = "Cliente",
            Fecha = DateTime.Today.AddDays(1),
            HoraInicio = new TimeSpan(9, 0, 0),
            HoraFin = new TimeSpan(9, 30, 0)
        };

        var envioLocal = await servicio.EnviarReservaCreada(reserva, "token-confirmar", "token-cancelar", CancellationToken.None);
        Assert.True(envioLocal);
        Assert.Equal(1, clienteCorreo.Envios);
        Assert.Contains("http://localhost:8080/api/Reservas/acciones/confirmar/token-confirmar", clienteCorreo.UltimoHtml);
        Assert.Contains("http://localhost:8080/api/Reservas/acciones/cancelar/token-cancelar", clienteCorreo.UltimoHtml);

        configuracion["Correo:UrlBaseApi"] = "https://api.optica.example.test";
        var envioPublico = await servicio.EnviarReservaCreada(reserva, "token-confirmar", "token-cancelar", CancellationToken.None);

        Assert.True(envioPublico);
        Assert.Equal(2, clienteCorreo.Envios);
        Assert.Contains("https://api.optica.example.test/api/Reservas/acciones/confirmar/token-confirmar", clienteCorreo.UltimoHtml);
        Assert.Contains("https://api.optica.example.test/api/Reservas/acciones/cancelar/token-cancelar", clienteCorreo.UltimoHtml);
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
        var servicio = new RecordatorioReservasService(repositorio, correo, new ColaCorreosReprogramacion(), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordatorioReservasService>.Instance);

        await servicio.EnviarPendientes(CancellationToken.None);
        await servicio.EnviarPendientes(CancellationToken.None);

        Assert.Equal(2, correo.RecordatoriosEnviados.Count);
        Assert.Equal(1, correo.RecordatoriosEnviados[0].Id);
        Assert.Equal(new[] { 1, 3 }, correo.RecordatoriosEnviados.Select(r => r.Id).Order().ToArray());
        Assert.Equal(2, repositorio.RecordatoriosMarcados.Count);
    }

    [Fact]
    public async Task CicloAutomatico_ActualizaVencidasConHoraChileAntesDeProcesarRecordatorios()
    {
        var ahoraUtc = new DateTimeOffset(2026, 1, 15, 13, 0, 0, TimeSpan.Zero);
        var reloj = new RelojPrueba(ahoraUtc);
        var repositorio = new ReservaRepositorioPrueba();
        var servicio = new RecordatorioReservasService(
            repositorio,
            new ReservaCorreoPrueba(),
            new ColaCorreosReprogramacion(),
            reloj,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordatorioReservasService>.Instance);

        await servicio.EnviarPendientes(CancellationToken.None);

        Assert.Equal(1, repositorio.ActualizacionesVencidas);
        Assert.Equal(new DateTime(2026, 1, 15), repositorio.AhoraChileRecibida?.Date);
        Assert.Equal(new TimeSpan(10, 0, 0), repositorio.AhoraChileRecibida?.TimeOfDay);
        Assert.Equal(new[] { "actualizar", "recordatorios" }, repositorio.OrdenOperaciones);
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

    private sealed class RelojPrueba(DateTimeOffset ahoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahoraUtc;
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
    public async Task ReprogramarReserva_EnviaCorreoConHorarioNuevoYNuevosTokens()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaId = 25,
            ClienteId = 8,
            HorarioId = 15,
            EstadoReserva = "Confirmada",
            TokenConfirmacionHash = TokenAccionReserva.ObtenerHash("token-confirmacion-anterior"),
            TokenCancelacionHash = TokenAccionReserva.ObtenerHash("token-cancelacion-anterior"),
            DatosCorreo = new ReservaCorreoDto
            {
                Id = 25,
                NombreCliente = "Ana Pérez",
                CorreoCliente = "ana@example.com",
                Fecha = new DateTime(2026, 11, 14),
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(9, 30, 0)
            },
            Horarios =
            [
                new() { Id = 15, Estado = "Inhabilitada", Fecha = new DateTime(2026, 11, 14), HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(9, 30, 0) },
                new() { Id = 27, Estado = "Habilitada", Fecha = new DateTime(2026, 11, 16), HoraInicio = new TimeSpan(11, 0, 0), HoraFin = new TimeSpan(11, 30, 0) }
            ]
        };
        var correo = new ReservaCorreoPrueba();
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance, new ColaCorreosReprogramacion());

        var correoEnviado = await servicio.ReprogramarReserva(25, 27, CancellationToken.None);

        Assert.True(correoEnviado);
        Assert.Equal("Pendiente", repositorio.EstadoReserva);
        Assert.Equal(25, correo.ReservaReprogramada?.Id);
        Assert.Equal(new DateTime(2026, 11, 16), correo.ReservaReprogramada?.Fecha);
        Assert.Equal(new TimeSpan(11, 0, 0), correo.ReservaReprogramada?.HoraInicio);
        Assert.Equal(43, correo.TokenConfirmacion?.Length);
        Assert.Equal(43, correo.TokenCancelacion?.Length);
        Assert.Equal(TokenAccionReserva.ObtenerHash(correo.TokenConfirmacion!), repositorio.TokenConfirmacionHash);
        Assert.Equal(TokenAccionReserva.ObtenerHash(correo.TokenCancelacion!), repositorio.TokenCancelacionHash);
        Assert.False(await repositorio.SonTokensReprogramacionVigentes(25,
            TokenAccionReserva.ObtenerHash("token-confirmacion-anterior"),
            TokenAccionReserva.ObtenerHash("token-cancelacion-anterior"),
            DateTime.UtcNow,
            CancellationToken.None));
    }

    [Fact]
    public async Task ReprogramarReserva_CorreoFallidoQuedaEnColaYSeReintenta()
    {
        var fechaNueva = new DateTime(2026, 11, 16);
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaId = 25,
            ClienteId = 8,
            HorarioId = 15,
            DatosCorreo = new ReservaCorreoDto { Id = 25, CorreoCliente = "ana@example.com" },
            Horarios = [new() { Id = 15, Estado = "Inhabilitada" }, new() { Id = 27, Estado = "Habilitada", Fecha = fechaNueva, HoraInicio = new TimeSpan(11, 0, 0), HoraFin = new TimeSpan(11, 30, 0) }]
        };
        var correo = new ReservaCorreoPrueba { EnvioCorrecto = false };
        var cola = new ColaCorreosReprogramacion();
        var servicio = new ReservaService(repositorio, correo, TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservaService>.Instance, cola);

        var enviadoAlPrimerIntento = await servicio.ReprogramarReserva(25, 27, CancellationToken.None);
        Assert.False(enviadoAlPrimerIntento);
        Assert.Equal(1, cola.Cantidad);
        Assert.Equal(27, repositorio.HorarioId);

        correo.EnvioCorrecto = true;
        var recordatorios = new RecordatorioReservasService(repositorio, correo, cola, TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RecordatorioReservasService>.Instance);
        await recordatorios.EnviarPendientes(CancellationToken.None);

        Assert.Equal(0, cola.Cantidad);
        Assert.Equal(fechaNueva, correo.ReservaReprogramada?.Fecha);
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
    public async Task ReprogramarReserva_RechazaFechaConOtraReservaActivaDelCliente()
    {
        var repositorio = new ReservaRepositorioPrueba
        {
            ReservaId = 25, ClienteId = 8, HorarioId = 15, ReservaActivaEseDia = true,
            Horarios = [new() { Id = 15, Estado = "Inhabilitada" }, new() { Id = 27, Estado = "Habilitada" }]
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).ReprogramarReserva(25, 27, CancellationToken.None));

        Assert.Contains("reserva activa para esta fecha", error.Message);
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
    public async Task CrearReserva_RechazaClienteConReservaActivaEseDia()
    {
        var repositorio = new ReservaRepositorioPrueba { ReservaActivaEseDia = true };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CrearServicio(repositorio).CrearReserva(new CrearReservaDto
        {
            NombreCompleto = "Ana Pérez", Rut = "12.345.678-5", Telefono = "9 1234 5678",
            Correo = "ana@example.com", IdHorario = 12
        }));

        Assert.Contains("reserva activa para esta fecha", error.Message);
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

internal sealed class ServicioEmailPrueba : IEmailService
{
    public int Envios { get; private set; }
    public string? UltimoHtml { get; private set; }

    public Task<bool> EnviarAsync(string? destinatario, string asunto, string cuerpoHtml, CancellationToken cancellationToken,
        bool importante = false, byte[]? adjunto = null, string? nombreAdjunto = null, string? tipoAdjunto = null)
    {
        Envios++;
        UltimoHtml = cuerpoHtml;
        return Task.FromResult(true);
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
    public bool ReservaActivaEseDia { get; set; }
    public int ActualizacionesVencidas { get; private set; }
    public DateTimeOffset? AhoraChileRecibida { get; private set; }
    public List<string> OrdenOperaciones { get; } = [];
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
        if (ReservaActivaEseDia) throw new InvalidOperationException("Ya tienes una reserva activa para esta fecha. No puedes reservar otra hora el mismo día.");
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

    public Task<IReadOnlyList<ReservaCorreoDto>> ObtenerReservasParaRecordatorio(DateTimeOffset ahoraChile, CancellationToken cancellationToken)
    {
        OrdenOperaciones.Add("recordatorios");
        return Task.FromResult<IReadOnlyList<ReservaCorreoDto>>(Recordatorios.Where(r => r.Estado is "Pendiente" or "Confirmada" && !RecordatoriosMarcados.Contains(r.Id)).ToList());
    }

    public Task<int> MarcarReservasVencidasComoRealizadas(DateTimeOffset ahoraChile, CancellationToken cancellationToken)
    {
        AhoraChileRecibida = ahoraChile;
        ActualizacionesVencidas++;
        OrdenOperaciones.Add("actualizar");
        return Task.FromResult(0);
    }

    public Task<bool> MarcarRecordatorioEnviado(int id, int horarioId, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        if (RecordatoriosMarcados.Contains(id) || !Recordatorios.Any(r => r.Id == id && r.HorarioId == horarioId && r.Estado is "Pendiente" or "Confirmada")) return Task.FromResult(false);
        RecordatoriosMarcados.Add(id);
        return Task.FromResult(true);
    }

    public Task DesmarcarRecordatorio(int id, int horarioId, DateTime enviadoUtc, CancellationToken cancellationToken)
    {
        RecordatoriosMarcados.Remove(id);
        return Task.CompletedTask;
    }

    public Task<bool> ExisteReserva(DateTime fecha, TimeSpan hora) => Task.FromResult(false);
    public Task<Reserva> Crear(Reserva reserva) => Task.FromResult(reserva);
    public Task<Reserva?> ObtenerPorId(int id) => Task.FromResult<Reserva?>(null);
    public Task<bool> CancelarReserva(int id, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> SonTokensReprogramacionVigentes(int id, string tokenConfirmacionHash, string tokenCancelacionHash, DateTime ahoraUtc, CancellationToken cancellationToken) =>
        Task.FromResult(ReservaId == id && TokenConfirmacionHash == tokenConfirmacionHash
            && TokenCancelacionHash == tokenCancelacionHash && TokenExpiraUtc > ahoraUtc
            && EstadoReserva is "Pendiente" or "Confirmada");

    public Task<bool> ReprogramarReserva(int id, int idHorario, string tokenConfirmacionHash, string tokenCancelacionHash, CancellationToken cancellationToken)
    {
        if (ReservaId != id) return Task.FromResult(false);
        var horario = Horarios.SingleOrDefault(h => h.Id == idHorario);
        if (horario is null) throw new KeyNotFoundException("El horario seleccionado no existe.");
        if (idHorario == HorarioId) throw new InvalidOperationException("El nuevo horario debe ser distinto al horario actual.");
        if (horario.Estado != "Habilitada" || HorarioOcupado)
            throw new InvalidOperationException(horario.Estado != "Habilitada" ? "El horario seleccionado no está disponible." : "El horario seleccionado ya no está disponible.");
        if (ReservaActivaEseDia)
            throw new InvalidOperationException("Ya tienes una reserva activa para esta fecha. No puedes reservar otra hora el mismo día.");
        var anterior = Horarios.Single(h => h.Id == HorarioId);
        anterior.Estado = "Habilitada";
        horario.Estado = "Inhabilitada";
        HorarioId = idHorario;
        EstadoReserva = "Pendiente";
        TokenConfirmacionHash = tokenConfirmacionHash;
        TokenCancelacionHash = tokenCancelacionHash;
        if (DatosCorreo is not null)
        {
            DatosCorreo.HorarioId = idHorario;
            DatosCorreo.Fecha = horario.Fecha;
            DatosCorreo.HoraInicio = horario.HoraInicio;
            DatosCorreo.HoraFin = horario.HoraFin;
        }
        return Task.FromResult(true);
    }
}

internal sealed class ReservaCorreoPrueba : IReservaCorreoService
{
    public ReservaCorreoDto? ReservaCreada { get; private set; }
    public ReservaCorreoDto? ReservaReprogramada { get; private set; }
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

    public Task<bool> EnviarReservaReprogramada(ReservaCorreoDto reserva, string tokenConfirmacion, string tokenCancelacion, CancellationToken cancellationToken)
    {
        ReservaReprogramada = reserva;
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
