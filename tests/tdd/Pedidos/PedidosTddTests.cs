using System.Net;
using System.Net.Http.Json;
using Optica.Api.Tdd.Tests.Soporte;

namespace Optica.Api.Tdd.Tests.Pedidos;

public class PedidosTddTests : IClassFixture<OpticaApiFactory>
{
    private readonly HttpClient _cliente;

    public PedidosTddTests(OpticaApiFactory fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task RegistrarPedido_ParaCliente_GuardaElPedidoConEstadoEnPreparacion()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/pedidos", new
        {
            clienteId = 1,
            productoId = 1,
            observacion = "Lentes monofocales"
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Contains("En preparación", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CambiarEstadoPedido_ActualizaYGuardaElNuevoEstado()
    {
        var respuesta = await _cliente.PatchAsJsonAsync("/api/pedidos/1/estado", new
        {
            estado = "Listo para entrega"
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Listo para entrega", await respuesta.Content.ReadAsStringAsync());
    }
}
