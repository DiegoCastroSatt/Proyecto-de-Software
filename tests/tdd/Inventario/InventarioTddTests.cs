using System.Net;
using System.Net.Http.Json;
using Optica.Api.Tdd.Tests.Soporte;
using Xunit;

namespace Optica.Api.Tdd.Tests.Inventario;

public class InventarioTddTests : IClassFixture<OpticaApiFactory>
{
    private readonly HttpClient _cliente;

    public InventarioTddTests(OpticaApiFactory fabrica) => _cliente = fabrica.CreateClient();

    [Fact]
    public async Task RegistrarProducto_ConDatosObligatorios_LoDejaVisibleEnInventario()
    {
        var identificador = "ARM-001";
        var respuesta = await _cliente.PostAsJsonAsync("/api/productos", new
        {
            identificador,
            nombre = "Armazón clásico",
            categoria = "Armazones",
            precio = 39990,
            stock = 4
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var inventario = await _cliente.GetAsync("/api/productos");
        Assert.Equal(HttpStatusCode.OK, inventario.StatusCode);
        Assert.Contains(identificador, await inventario.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConsultarInventario_ConProductoSinStock_LoIndicaComoSinExistencias()
    {
        var respuesta = await _cliente.GetAsync("/api/productos?soloSinStock=true");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Sin stock", await respuesta.Content.ReadAsStringAsync());
    }
}
