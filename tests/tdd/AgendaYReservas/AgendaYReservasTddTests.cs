using Xunit;

using System.Net;
using System.Net.Http.Json;
using Optica.Api.Tdd.Tests.Soporte;

namespace Optica.Api.Tdd.Tests.AgendaYReservas;

public class AgendaYReservasTddTests : IClassFixture<OpticaApiFactory>
{
    private readonly HttpClient _cliente;

    public AgendaYReservasTddTests(OpticaApiFactory fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task RegistrarReserva_ConHoraDisponible_GuardaLaHoraEnLaAgenda()
    {
        var fecha = DateTime.Today.AddDays(1);
        var respuesta = await _cliente.PostAsJsonAsync("/api/reservas", new
        {
            nombreCompleto = "Cliente de prueba",
            rut = "12.345.678-9",
            telefono = "+56 9 1234 5678",
            correo = "cliente@ejemplo.cl",
            fecha,
            hora = "14:00:00"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task RegistrarReserva_ConHoraOcupada_RechazaLaSegundaSolicitud()
    {
        var fecha = DateTime.Today.AddDays(2);
        var solicitud = new
        {
            nombreCompleto = "Cliente de prueba",
            rut = "12.345.678-9",
            telefono = "+56 9 1234 5678",
            correo = "cliente@ejemplo.cl",
            fecha,
            hora = "15:00:00"
        };
        await _cliente.PostAsJsonAsync("/api/reservas", solicitud);

        var respuesta = await _cliente.PostAsJsonAsync("/api/reservas", solicitud);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("ya está reservada", await respuesta.Content.ReadAsStringAsync());
    }
}
