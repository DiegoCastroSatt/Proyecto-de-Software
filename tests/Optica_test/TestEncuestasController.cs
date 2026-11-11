using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Encuestas.Controllers;
using Optica.Api.Modules.Encuestas.DTOs;
using Optica.Api.Modules.Encuestas.Interfaces;
using Optica.Api.Modules.Encuestas.Models;
using Optica.Api.Modules.Encuestas.Services;
using Xunit;

namespace Optica.Tests;

// Pruebas HTTP del controlador de encuestas: permisos, endpoints públicos y validación de parámetros.
public sealed class TestEncuestasController : IAsyncLifetime
{
    private const string TokenVigente = "token-de-prueba-vigente";
    private const string TokenVencido = "token-de-prueba-vencido";
    private const string RangoValido = "desde=2026-01-01&hasta=2026-12-31";

    private IHost servidor = null!;

    public async Task InitializeAsync()
    {
        var nombreDb = Guid.NewGuid().ToString();
        servidor = await new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(s =>
        {
            s.AddDbContext<OpticaDbContext>(o => o.UseInMemoryDatabase(nombreDb)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            s.AddSingleton(TimeProvider.System);
            s.AddScoped<IEncuestaService, EncuestaService>();
            s.AddLogging();
            s.AddControllers().AddApplicationPart(typeof(EncuestasController).Assembly);
            s.AddAuthentication("Pruebas")
                .AddScheme<AuthenticationSchemeOptions, AutenticacionPrueba>("Pruebas", _ => { });
            s.AddAuthorization(PermisosAcceso.Configurar);
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(e => e.MapControllers());
        })).StartAsync();

        using var scope = servidor.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
        var horario = new Horario { Id = 1, Fecha = new DateTime(2026, 11, 10), HoraInicio = new TimeSpan(10, 0, 0), HoraFin = new TimeSpan(11, 0, 0), Estado = "Habilitada" };
        db.Horarios.Add(horario);
        db.Reservas.AddRange(
            new Reserva { Id = 1, ClienteId = 1, HorarioId = 1, Estado = "Realizada" },
            new Reserva { Id = 2, ClienteId = 1, HorarioId = 1, Estado = "Realizada" });
        db.EncuestasAtencion.AddRange(
            new EncuestaAtencion { ReservaId = 1, TokenHash = TokenAccionReserva.ObtenerHash(TokenVigente), TokenExpiraUtc = DateTime.UtcNow.AddDays(10), FechaEnvioUtc = DateTime.UtcNow },
            new EncuestaAtencion { ReservaId = 2, TokenHash = TokenAccionReserva.ObtenerHash(TokenVencido), TokenExpiraUtc = DateTime.UtcNow.AddMinutes(-1), FechaEnvioUtc = DateTime.UtcNow.AddDays(-15) });
        db.RespuestasEncuestaAtencion.Add(new RespuestaEncuestaAtencion
        {
            Id = 50, CalificacionGeneral = 1, CalificacionTrato = 1, CalificacionPuntualidad = 1,
            Comentario = "Comentario inapropiado", FechaRespuesta = new DateTime(2026, 11, 5)
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await servidor.StopAsync();
        servidor.Dispose();
    }

    private HttpClient Cliente(string tipo = "", params string[] permisos)
    {
        var cliente = servidor.GetTestClient();
        if (!string.IsNullOrEmpty(tipo)) cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", tipo);
        if (permisos.Length > 0) cliente.DefaultRequestHeaders.Add("X-Prueba-Permisos", string.Join(",", permisos));
        return cliente;
    }

    private static object RespuestaValida() => new { calificacionGeneral = 5, calificacionTrato = 4, calificacionPuntualidad = 3, comentario = "Bien" };

    private T ConDb<T>(Func<OpticaDbContext, T> consulta)
    {
        using var scope = servidor.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<OpticaDbContext>());
    }

    [Theory]
    [InlineData("", "", HttpStatusCode.Unauthorized)]
    [InlineData("cliente", "agenda", HttpStatusCode.Forbidden)]
    [InlineData("Trabajador", "ventas", HttpStatusCode.Forbidden)]
    [InlineData("Trabajador", "agenda", HttpStatusCode.OK)]
    [InlineData("Administrador", "", HttpStatusCode.OK)]
    public async Task ResultadosRequierenPermisoDeAgenda(string tipo, string permiso, HttpStatusCode esperado)
    {
        using var cliente = Cliente(tipo, string.IsNullOrEmpty(permiso) ? [] : [permiso]);

        using var resumen = await cliente.GetAsync($"/api/Encuestas/resumen?{RangoValido}");
        using var comentarios = await cliente.GetAsync($"/api/Encuestas/comentarios?{RangoValido}");

        Assert.Equal(esperado, resumen.StatusCode);
        Assert.Equal(esperado, comentarios.StatusCode);
    }

    [Theory]
    [InlineData("", "", HttpStatusCode.Unauthorized)]
    [InlineData("Trabajador", "ventas", HttpStatusCode.Forbidden)]
    public async Task OcultarComentarioRequierePermisoDeAgenda(string tipo, string permiso, HttpStatusCode esperado)
    {
        using var cliente = Cliente(tipo, string.IsNullOrEmpty(permiso) ? [] : [permiso]);

        using var respuesta = await cliente.PutAsJsonAsync("/api/Encuestas/respuestas/50/comentario-oculto", new { oculto = true });

        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.False(ConDb(db => db.RespuestasEncuestaAtencion.Single().ComentarioOculto));
    }

    [Fact]
    public async Task OcultarYMostrarComentarioConPermisoDeAgenda()
    {
        using var cliente = Cliente("Trabajador", "agenda");

        using var ocultar = await cliente.PutAsJsonAsync("/api/Encuestas/respuestas/50/comentario-oculto", new { oculto = true });
        var oculto = await cliente.GetFromJsonAsync<ComentariosEncuestasPaginadosDto>($"/api/Encuestas/comentarios?{RangoValido}");
        using var mostrar = await cliente.PutAsJsonAsync("/api/Encuestas/respuestas/50/comentario-oculto", new { oculto = false });
        var visible = await cliente.GetFromJsonAsync<ComentariosEncuestasPaginadosDto>($"/api/Encuestas/comentarios?{RangoValido}");

        Assert.Equal(HttpStatusCode.NoContent, ocultar.StatusCode);
        var itemOculto = Assert.Single(oculto!.Elementos);
        Assert.True(itemOculto.ComentarioOculto);
        Assert.Null(itemOculto.Comentario);
        Assert.Equal(HttpStatusCode.NoContent, mostrar.StatusCode);
        Assert.Equal("Comentario inapropiado", Assert.Single(visible!.Elementos).Comentario);
    }

    [Fact]
    public async Task OcultarComentarioInexistenteDevuelve404()
    {
        using var cliente = Cliente("Administrador");

        using var respuesta = await cliente.PutAsJsonAsync("/api/Encuestas/respuestas/999/comentario-oculto", new { oculto = true });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task EncuestaPublicaSeConsultaYRespondeSinSesionUnaSolaVez()
    {
        using var cliente = Cliente();

        using var consulta = await cliente.GetAsync($"/api/Encuestas/responder/{TokenVigente}");
        var encuesta = await consulta.Content.ReadFromJsonAsync<EncuestaPublicaDto>();
        using var primera = await cliente.PostAsJsonAsync($"/api/Encuestas/responder/{TokenVigente}", RespuestaValida());
        using var segunda = await cliente.PostAsJsonAsync($"/api/Encuestas/responder/{TokenVigente}", RespuestaValida());
        using var consultaPosterior = await cliente.GetAsync($"/api/Encuestas/responder/{TokenVigente}");

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(new EncuestaPublicaDto("2026-11-10", "10:00"), encuesta);
        Assert.Equal("no-store", consulta.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, segunda.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultaPosterior.StatusCode);
        Assert.Equal(2, ConDb(db => db.RespuestasEncuestaAtencion.Count()));
    }

    [Fact]
    public async Task EnlaceVencidoDevuelve410()
    {
        using var cliente = Cliente();

        using var consulta = await cliente.GetAsync($"/api/Encuestas/responder/{TokenVencido}");
        using var respuesta = await cliente.PostAsJsonAsync($"/api/Encuestas/responder/{TokenVencido}", RespuestaValida());

        Assert.Equal(HttpStatusCode.Gone, consulta.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, respuesta.StatusCode);
    }

    [Fact]
    public async Task RespuestaInvalidaDevuelve400ConMensaje()
    {
        using var cliente = Cliente();

        using var respuesta = await cliente.PostAsJsonAsync($"/api/Encuestas/responder/{TokenVigente}",
            new { calificacionGeneral = 6, calificacionTrato = 4, calificacionPuntualidad = 3 });
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("1 a 5 estrellas", cuerpo!["mensaje"]);
        Assert.NotNull(ConDb(db => db.EncuestasAtencion.Single(e => e.ReservaId == 1).TokenHash));
    }

    [Theory]
    [InlineData("/api/Encuestas/resumen")]
    [InlineData("/api/Encuestas/resumen?desde=2026-01-01")]
    [InlineData("/api/Encuestas/resumen?desde=01-01-2026&hasta=31-12-2026")]
    [InlineData("/api/Encuestas/resumen?desde=2026-12-31&hasta=2026-01-01")]
    [InlineData("/api/Encuestas/resumen?desde=2020-01-01&hasta=2026-01-01")]
    [InlineData("/api/Encuestas/comentarios?desde=2026-01-01&hasta=2026-12-31&pagina=0")]
    [InlineData("/api/Encuestas/comentarios?desde=2026-01-01&hasta=2026-12-31&tamanoPagina=100")]
    [InlineData("/api/Encuestas/comentarios?desde=2026-01-01&hasta=2026-12-31&calificacion=7")]
    public async Task ParametrosInvalidosDevuelven400(string ruta)
    {
        using var cliente = Cliente("Administrador");

        using var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
