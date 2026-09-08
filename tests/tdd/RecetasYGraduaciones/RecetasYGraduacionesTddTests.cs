using System.Net;
using System.Net.Http.Json;
using Optica.Api.Tdd.Tests.Soporte;

namespace Optica.Api.Tdd.Tests.RecetasYGraduaciones;

public class RecetasYGraduacionesTddTests : IClassFixture<OpticaApiFactory>
{
    private readonly HttpClient _cliente;

    public RecetasYGraduacionesTddTests(OpticaApiFactory fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task RegistrarRecetaEscrita_ParaClienteSeleccionado_GuardaLaReceta()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/recetas", new
        {
            clienteId = 1,
            contenido = "Uso permanente. Control en seis meses."
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task RegistrarRecetaConImagen_ParaClienteSeleccionado_GuardaLaReferenciaDeImagen()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/recetas", new
        {
            clienteId = 1,
            imagen = "recetas/cliente-1.jpg"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task RegistrarGraduacion_DeAmbosOjos_GuardaLosDatosOpticosDelCliente()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/graduaciones", new
        {
            clienteId = 1,
            ojoDerecho = new { esfera = -1.25, cilindro = -0.50, eje = 90 },
            ojoIzquierdo = new { esfera = -1.00, cilindro = -0.25, eje = 85 }
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }
}
