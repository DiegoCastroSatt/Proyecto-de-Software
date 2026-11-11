using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Encuestas.DTOs;
using Optica.Api.Modules.Encuestas.Interfaces;
using Optica.Api.Modules.Encuestas.Models;
using Optica.Api.Modules.Encuestas.Services;
using Xunit;

namespace Optica.Tests;

// Pruebas de las encuestas de satisfacción por atención.
public sealed class TestEncuestas : IDisposable
{
    // 15:00 UTC del 11/11/2026 equivale a las 12:00 en Chile (UTC-3 en horario de verano).
    private static readonly DateTimeOffset Ahora = new(2026, 11, 11, 15, 0, 0, TimeSpan.Zero);

    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private readonly IDataProtectionProvider proteccion = new EphemeralDataProtectionProvider();
    private readonly RelojAjustable reloj = new(Ahora);

    public void Dispose() => db.Dispose();

    private SolicitudEncuestasService Programador()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Correo:UrlBaseFrontend"] = "http://localhost:4200" })
            .Build();
        return new SolicitudEncuestasService(db, new EncuestaCorreoService(configuracion, proteccion), reloj,
            NullLogger<SolicitudEncuestasService>.Instance);
    }

    private EncuestaService Servicio() => new(db, reloj);

    private async Task<Reserva> CrearAtencionAsync(
        DateTime fecha, TimeSpan horaFin, string estado = "Realizada", string? correo = "ana@example.test")
    {
        var cliente = new Cliente { Rut = $"{Guid.NewGuid():N}"[..10], Nombre = "Ana", Apellido = "Pérez", Correo = correo, Estado = "Activo" };
        var horario = new Horario { Fecha = fecha, HoraInicio = horaFin - TimeSpan.FromHours(1), HoraFin = horaFin, Estado = "Habilitada" };
        db.Clientes.Add(cliente);
        db.Horarios.Add(horario);
        await db.SaveChangesAsync();
        var reserva = new Reserva { ClienteId = cliente.IdCliente, HorarioId = horario.Id, Estado = estado };
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        return reserva;
    }

    /// <summary>Crea una atención de ayer, programa su encuesta y devuelve el token enviado por correo.</summary>
    private async Task<string> CrearEncuestaAsync()
    {
        await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11));
        await Programador().ProgramarPendientes(CancellationToken.None);
        return ObtenerTokenDelCorreo(await db.CorreosReserva.SingleAsync());
    }

    private string ObtenerTokenDelCorreo(CorreoReservaPendiente correo)
    {
        var html = proteccion.CreateProtector("Optica.CorreosReservas.v1").Unprotect(correo.CuerpoHtmlProtegido);
        return Uri.UnescapeDataString(Regex.Match(html, "/encuesta\\?token=([^\"&]+)").Groups[1].Value);
    }

    private static ResponderEncuestaDto Respuesta(int general = 5, int trato = 4, int puntualidad = 3, string? comentario = "Muy buena atención") =>
        new() { CalificacionGeneral = general, CalificacionTrato = trato, CalificacionPuntualidad = puntualidad, Comentario = comentario };

    [Fact]
    public async Task Programar_CreaEncuestaYCorreoParaAtencionRealizadaReciente()
    {
        var reserva = await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11));

        var creadas = await Programador().ProgramarPendientes(CancellationToken.None);

        Assert.Equal(1, creadas);
        var encuesta = await db.EncuestasAtencion.SingleAsync();
        var correo = await db.CorreosReserva.SingleAsync();
        Assert.Equal(reserva.Id, encuesta.ReservaId);
        Assert.False(encuesta.Respondida);
        Assert.Equal(EncuestaCorreoService.TipoCorreo, correo.Tipo);
        Assert.Equal("ana@example.test", correo.Destinatario);
        // La atención terminó hace más de 2 horas: el correo se programa para ahora.
        Assert.Equal(Ahora.UtcDateTime, correo.ProximoIntentoUtc);
        Assert.Equal(Ahora.UtcDateTime, encuesta.FechaEnvioUtc);
        Assert.Equal(Ahora.UtcDateTime.AddDays(14), encuesta.TokenExpiraUtc);
        // Solo se guarda el hash del token enviado por correo.
        var token = ObtenerTokenDelCorreo(correo);
        Assert.Equal(TokenAccionReserva.ObtenerHash(token), encuesta.TokenHash);
    }

    [Fact]
    public async Task Programar_EnviaDosHorasDespuesDelFinDeLaAtencion()
    {
        // Termina a las 11:30 de Chile (14:30 UTC), media hora antes de "ahora".
        await CrearAtencionAsync(new DateTime(2026, 11, 11), new TimeSpan(11, 30, 0));

        await Programador().ProgramarPendientes(CancellationToken.None);

        var correo = await db.CorreosReserva.SingleAsync();
        Assert.Equal(new DateTime(2026, 11, 11, 16, 30, 0, DateTimeKind.Utc), correo.ProximoIntentoUtc);
    }

    [Fact]
    public async Task Programar_SoloCreaUnaEncuestaPorAtencion()
    {
        await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11));

        await Programador().ProgramarPendientes(CancellationToken.None);
        var segunda = await Programador().ProgramarPendientes(CancellationToken.None);

        Assert.Equal(0, segunda);
        Assert.Single(await db.EncuestasAtencion.ToListAsync());
        Assert.Single(await db.CorreosReserva.ToListAsync());
    }

    [Fact]
    public async Task Programar_IgnoraReservasNoRealizadasSinCorreoAntiguasOFuturas()
    {
        await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11), estado: "Confirmada");
        await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11), estado: "Cancelada");
        await CrearAtencionAsync(new DateTime(2026, 11, 10), TimeSpan.FromHours(11), correo: null);
        await CrearAtencionAsync(new DateTime(2026, 11, 7), TimeSpan.FromHours(11));
        await CrearAtencionAsync(new DateTime(2026, 11, 11), TimeSpan.FromHours(18));

        var creadas = await Programador().ProgramarPendientes(CancellationToken.None);

        Assert.Equal(0, creadas);
        Assert.Empty(await db.EncuestasAtencion.ToListAsync());
    }

    [Fact]
    public async Task ObtenerPorToken_DevuelveSoloFechaYHoraDeLaAtencion()
    {
        var token = await CrearEncuestaAsync();

        var (resultado, encuesta) = await Servicio().ObtenerPorToken(token, CancellationToken.None);

        Assert.Equal(ResultadoEncuesta.Disponible, resultado);
        Assert.Equal(new EncuestaPublicaDto("2026-11-10", "10:00"), encuesta);
    }

    [Theory]
    [InlineData("")]
    [InlineData("token-que-no-existe")]
    public async Task ObtenerPorToken_RechazaTokenInvalido(string token)
    {
        await CrearEncuestaAsync();

        var (resultado, encuesta) = await Servicio().ObtenerPorToken(token, CancellationToken.None);

        Assert.Equal(ResultadoEncuesta.NoEncontrada, resultado);
        Assert.Null(encuesta);
    }

    [Fact]
    public async Task TokenVencidoSeRechazaAlConsultarYAlResponder()
    {
        var token = await CrearEncuestaAsync();
        reloj.Ahora = Ahora.AddDays(15);

        var (consulta, _) = await Servicio().ObtenerPorToken(token, CancellationToken.None);
        var respuesta = await Servicio().Responder(token, Respuesta(), CancellationToken.None);

        Assert.Equal(ResultadoEncuesta.Vencida, consulta);
        Assert.Equal(ResultadoEncuesta.Vencida, respuesta);
        Assert.Empty(await db.RespuestasEncuestaAtencion.ToListAsync());
    }

    [Fact]
    public async Task Responder_GuardaRespuestaAnonimaYAnulaElToken()
    {
        var token = await CrearEncuestaAsync();

        var resultado = await Servicio().Responder(token, Respuesta(comentario: "  Muy buena atención  "), CancellationToken.None);

        Assert.Equal(ResultadoEncuesta.Respondida, resultado);
        var encuesta = await db.EncuestasAtencion.SingleAsync();
        Assert.True(encuesta.Respondida);
        Assert.Null(encuesta.TokenHash);
        Assert.Null(encuesta.TokenExpiraUtc);
        var guardada = await db.RespuestasEncuestaAtencion.SingleAsync();
        Assert.Equal((5, 4, 3), (guardada.CalificacionGeneral, guardada.CalificacionTrato, guardada.CalificacionPuntualidad));
        Assert.Equal("Muy buena atención", guardada.Comentario);
        Assert.Equal(new DateTime(2026, 11, 11), guardada.FechaRespuesta);
        // La respuesta no tiene ninguna referencia a la reserva, al cliente ni a la encuesta.
        var propiedades = typeof(RespuestaEncuestaAtencion).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain(propiedades, nombre => nombre.Contains("Reserva") || nombre.Contains("Cliente") || nombre.Contains("Encuesta"));
    }

    [Fact]
    public async Task Responder_SegundaRespuestaSeRechazaYNoCambiaLaPrimera()
    {
        var token = await CrearEncuestaAsync();
        await Servicio().Responder(token, Respuesta(general: 5), CancellationToken.None);

        var segunda = await Servicio().Responder(token, Respuesta(general: 1, comentario: "Otra"), CancellationToken.None);

        Assert.Equal(ResultadoEncuesta.NoEncontrada, segunda);
        var guardada = await db.RespuestasEncuestaAtencion.SingleAsync();
        Assert.Equal(5, guardada.CalificacionGeneral);
        Assert.Equal("Muy buena atención", guardada.Comentario);
    }

    [Fact]
    public async Task Responder_ComentarioVacioSeGuardaComoNulo()
    {
        var token = await CrearEncuestaAsync();

        await Servicio().Responder(token, Respuesta(comentario: "   "), CancellationToken.None);

        Assert.Null((await db.RespuestasEncuestaAtencion.SingleAsync()).Comentario);
    }

    [Theory]
    [InlineData(0, 4, 3, 10)]
    [InlineData(6, 4, 3, 10)]
    [InlineData(5, 0, 3, 10)]
    [InlineData(5, 4, 6, 10)]
    [InlineData(5, 4, 3, 1001)]
    public async Task Responder_RechazaDatosInvalidosSinGuardar(int general, int trato, int puntualidad, int largoComentario)
    {
        var token = await CrearEncuestaAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().Responder(
            token, Respuesta(general, trato, puntualidad, new string('a', largoComentario)), CancellationToken.None));

        Assert.Empty(await db.RespuestasEncuestaAtencion.ToListAsync());
        Assert.NotNull((await db.EncuestasAtencion.SingleAsync()).TokenHash);
    }

    [Fact]
    public async Task ObtenerResumen_CalculaTotalesPromediosDistribucionYEvolucion()
    {
        db.EncuestasAtencion.AddRange(
            new EncuestaAtencion { ReservaId = 1, FechaEnvioUtc = new DateTime(2026, 10, 5, 15, 0, 0, DateTimeKind.Utc), Respondida = true },
            new EncuestaAtencion { ReservaId = 2, FechaEnvioUtc = new DateTime(2026, 10, 6, 15, 0, 0, DateTimeKind.Utc), Respondida = false },
            new EncuestaAtencion { ReservaId = 3, FechaEnvioUtc = new DateTime(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc), Respondida = true },
            new EncuestaAtencion { ReservaId = 4, FechaEnvioUtc = new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc), Respondida = true },
            // Fuera del periodo y programada a futuro: no cuentan como enviadas.
            new EncuestaAtencion { ReservaId = 5, FechaEnvioUtc = new DateTime(2026, 8, 1, 15, 0, 0, DateTimeKind.Utc), Respondida = true },
            new EncuestaAtencion { ReservaId = 6, FechaEnvioUtc = Ahora.UtcDateTime.AddHours(1), Respondida = false });
        db.RespuestasEncuestaAtencion.AddRange(
            new RespuestaEncuestaAtencion { CalificacionGeneral = 4, CalificacionTrato = 5, CalificacionPuntualidad = 3, FechaRespuesta = new DateTime(2026, 10, 5) },
            new RespuestaEncuestaAtencion { CalificacionGeneral = 5, CalificacionTrato = 5, CalificacionPuntualidad = 4, FechaRespuesta = new DateTime(2026, 11, 2) },
            new RespuestaEncuestaAtencion { CalificacionGeneral = 2, CalificacionTrato = 4, CalificacionPuntualidad = 1, FechaRespuesta = new DateTime(2026, 11, 3) },
            new RespuestaEncuestaAtencion { CalificacionGeneral = 1, CalificacionTrato = 1, CalificacionPuntualidad = 1, FechaRespuesta = new DateTime(2026, 8, 1) });
        await db.SaveChangesAsync();

        var resumen = await Servicio().ObtenerResumen(new DateTime(2026, 9, 1), new DateTime(2026, 11, 11), CancellationToken.None);

        Assert.Equal(4, resumen.Enviadas);
        Assert.Equal(3, resumen.Respondidas);
        Assert.Equal(75, resumen.TasaRespuesta);
        Assert.Equal(3.67, resumen.PromedioGeneral);
        Assert.Equal(4.67, resumen.PromedioTrato);
        Assert.Equal(2.67, resumen.PromedioPuntualidad);
        Assert.Equal([5, 4, 3, 2, 1], resumen.DistribucionGeneral.Select(d => d.Estrellas));
        Assert.Equal([1, 1, 0, 1, 0], resumen.DistribucionGeneral.Select(d => d.Cantidad));
        Assert.Equal(["2026-10", "2026-11"], resumen.EvolucionMensual.Select(m => m.Mes));
        Assert.Equal(3.5, resumen.EvolucionMensual[1].PromedioGeneral);
        Assert.Equal(2, resumen.EvolucionMensual[1].Respuestas);
    }

    [Fact]
    public async Task ObtenerResumen_SinRespuestasDevuelvePromediosNulos()
    {
        var resumen = await Servicio().ObtenerResumen(new DateTime(2026, 9, 1), new DateTime(2026, 11, 11), CancellationToken.None);

        Assert.Equal(0, resumen.Enviadas);
        Assert.Equal(0, resumen.TasaRespuesta);
        Assert.Null(resumen.PromedioGeneral);
        Assert.Empty(resumen.EvolucionMensual);
    }

    [Fact]
    public async Task ObtenerComentarios_FiltraOrdenaYPagina()
    {
        for (var dia = 1; dia <= 12; dia++)
        {
            db.RespuestasEncuestaAtencion.Add(new RespuestaEncuestaAtencion
            {
                CalificacionGeneral = dia % 2 == 0 ? 5 : 3,
                CalificacionTrato = 4,
                CalificacionPuntualidad = 4,
                Comentario = dia == 12 ? null : $"Comentario {dia}",
                FechaRespuesta = new DateTime(2026, 11, dia)
            });
        }
        await db.SaveChangesAsync();
        var desde = new DateTime(2026, 11, 1);
        var hasta = new DateTime(2026, 11, 30);

        var conComentario = await Servicio().ObtenerComentarios(desde, hasta, null, true, 1, 10, CancellationToken.None);
        var segundaPagina = await Servicio().ObtenerComentarios(desde, hasta, null, true, 2, 10, CancellationToken.None);
        var cincoEstrellas = await Servicio().ObtenerComentarios(desde, hasta, 5, false, 1, 10, CancellationToken.None);

        Assert.Equal(11, conComentario.Total);
        Assert.Equal(10, conComentario.Elementos.Count);
        Assert.Equal("Comentario 11", conComentario.Elementos[0].Comentario);
        Assert.Equal("2026-11", conComentario.Elementos[0].Mes);
        Assert.Single(segundaPagina.Elementos);
        Assert.Equal(6, cincoEstrellas.Total);
        Assert.Null(cincoEstrellas.Elementos[0].Comentario);
    }

    [Fact]
    public async Task OcultarComentario_NoMuestraElTextoPeroSusCalificacionesSiguenContando()
    {
        db.RespuestasEncuestaAtencion.Add(new RespuestaEncuestaAtencion
        {
            Id = 7, CalificacionGeneral = 1, CalificacionTrato = 1, CalificacionPuntualidad = 1,
            Comentario = "Texto inapropiado", FechaRespuesta = new DateTime(2026, 11, 5)
        });
        await db.SaveChangesAsync();
        var desde = new DateTime(2026, 11, 1);
        var hasta = new DateTime(2026, 11, 30);

        Assert.True(await Servicio().CambiarVisibilidadComentario(7, true, CancellationToken.None));
        var comentarios = await Servicio().ObtenerComentarios(desde, hasta, null, true, 1, 10, CancellationToken.None);
        var resumen = await Servicio().ObtenerResumen(desde, hasta, CancellationToken.None);

        var item = Assert.Single(comentarios.Elementos);
        Assert.True(item.ComentarioOculto);
        Assert.Null(item.Comentario);
        Assert.Equal(1, resumen.Respondidas);
        Assert.Equal(1, resumen.PromedioGeneral);
        Assert.Equal("Texto inapropiado", (await db.RespuestasEncuestaAtencion.SingleAsync()).Comentario);
        Assert.False(await Servicio().CambiarVisibilidadComentario(999, true, CancellationToken.None));
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(6, 1, 10)]
    [InlineData(null, 0, 10)]
    [InlineData(null, 1, 51)]
    public async Task ObtenerComentarios_RechazaParametrosInvalidos(int? calificacion, int pagina, int tamano)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio().ObtenerComentarios(
            new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), calificacion, true, pagina, tamano, CancellationToken.None));
    }

    private sealed class RelojAjustable(DateTimeOffset ahora) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = ahora;
        public override DateTimeOffset GetUtcNow() => Ahora;
    }
}
