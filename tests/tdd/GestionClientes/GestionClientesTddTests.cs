using System.Net;
using System.Net.Http.Json;
using Optica.Api.Tdd.Tests.Soporte;

namespace Optica.Api.Tdd.Tests.GestionClientes;

public class GestionClientesTddTests : IClassFixture<OpticaApiFactory>
{
    private readonly HttpClient _cliente;

    public GestionClientesTddTests(OpticaApiFactory fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task RegistrarCliente_ConNombreRutYContacto_GuardaYDevuelveElClienteCreado()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/clientes", new
        {
            nombre = "María",
            apellido = "González",
            rut = "12.345.678-9",
            telefono = "+56 9 1234 5678",
            correo = "maria@ejemplo.cl"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Contains("12.345.678-9", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BuscarClientes_SinCoincidencias_IndicaQueNoHayResultados()
    {
        var respuesta = await _cliente.GetAsync("/api/clientes?busqueda=cliente-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("No hay resultados", await respuesta.Content.ReadAsStringAsync());
    }
}
