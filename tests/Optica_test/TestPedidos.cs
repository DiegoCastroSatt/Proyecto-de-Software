using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Pedidos.DTOs;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Services;
using Optica.Api.Modules.Pedidos.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestPedidos
{
    private static PedidoService CrearServicio(RepositorioPedidos repo, CorreoFalso? correo = null)
        => new(repo, correo ?? new CorreoFalso());
    [Fact]
    public async Task Crear_NormalizaRutYPreservaDatos()
    {
        var repo = new RepositorioPedidos();
        var fecha = new DateTime(2026, 10, 1);
        var resultado = await CrearServicio(repo).CrearPedidoAsync(new CreatePedidoDto
        { Rut = " 12.345.678-5 ", Fecha = fecha, Total = 25000, Anotaciones = "Armazón negro" });
        Assert.Equal("123456785", repo.RutConsultado);
        Assert.Equal("123456785", repo.Guardado!.Rut);
        Assert.Equal(42, resultado.IdPedido);
        Assert.Equal("Ana Pérez", resultado.NombreCliente);
        Assert.Equal(fecha, resultado.Fecha);
        Assert.Equal(25000, resultado.Total);
        Assert.Equal("Pendiente", resultado.Estado);
        Assert.Equal("Armazón negro", resultado.Anotaciones);
    }

    [Theory]
    [InlineData("123456785")]
    [InlineData("12345678-5")]
    [InlineData("12.345.678-5")]
    public async Task Crear_ConservaElRutAlmacenadoDelClienteParaRespetarLaClaveForanea(string rutAlmacenado)
    {
        var repo = new RepositorioPedidos { Cliente = new Cliente { Rut = rutAlmacenado, Nombre = "Ana", Apellido = "Pérez" } };
        await CrearServicio(repo).CrearPedidoAsync(new CreatePedidoDto { Rut = "12.345.678-5", Fecha = new DateTime(2026, 10, 24) });
        Assert.Equal("123456785", repo.RutConsultado);
        Assert.Equal(rutAlmacenado, repo.Guardado!.Rut);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12.345.678-9")]
    [InlineData("abc")]
    public async Task Crear_RechazaRutInvalidoSinConsultarNiGuardar(string rut)
    {
        var repo = new RepositorioPedidos();
        await Assert.ThrowsAsync<ArgumentException>(() => CrearServicio(repo).CrearPedidoAsync(new CreatePedidoDto { Rut = rut }));
        Assert.Null(repo.RutConsultado);
        Assert.Null(repo.Guardado);
    }

    [Fact]
    public async Task Crear_RechazaClienteNoActivoOInexistente()
    {
        var repo = new RepositorioPedidos { Cliente = null };
        await Assert.ThrowsAsync<ArgumentException>(() => CrearServicio(repo).CrearPedidoAsync(new CreatePedidoDto { Rut = "12345678-5" }));
        Assert.Null(repo.Guardado);
    }

    [Fact]
    public async Task Crear_AceptaTotalCeroYAnotacionesNulas()
    {
        var repo = new RepositorioPedidos();

        var resultado = await CrearServicio(repo).CrearPedidoAsync(new CreatePedidoDto
        {
            Rut = "12345678-5",
            Total = 0,
            Anotaciones = null
        });

        Assert.Equal(0, resultado.Total);
        Assert.Null(resultado.Anotaciones);
        Assert.Equal(0, repo.Guardado!.Total);
        Assert.Null(repo.Guardado.Anotaciones);
    }

    [Fact]
    public async Task ObtenerPedidos_DevuelvePedidosDelRepositorio()
    {
        var pedidos = new[]
        {
            new PedidoResponseDto { IdPedido = 1, NombreCliente = "Ana Pérez", Estado = "Pendiente" },
            new PedidoResponseDto { IdPedido = 2, NombreCliente = "Carlos Soto", Estado = "Listo" }
        };
        var repo = new RepositorioPedidos { PedidosDetalle = pedidos };

        var resultado = await CrearServicio(repo).ObtenerPedidosAsync();

        Assert.Equal(pedidos, resultado);
    }

    [Fact]
    public async Task ObtenerPedidoPorId_DevuelveNullSiNoExiste()
    {
        var resultado = await CrearServicio(new RepositorioPedidos())
            .ObtenerPedidoPorIdAsync(404);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObtenerClientesActivos_CombinaNombreYApellido()
    {
        var repo = new RepositorioPedidos
        {
            ClientesActivos =
            [
                new Cliente { Rut = "123456785", Nombre = "Ana", Apellido = "Pérez" },
                new Cliente { Rut = "987654321", Nombre = "Luis", Apellido = "Gómez" }
            ]
        };

        var resultado = (await CrearServicio(repo).ObtenerClientesActivosAsync()).ToArray();

        Assert.Collection(
            resultado,
            cliente =>
            {
                Assert.Equal("123456785", cliente.Rut);
                Assert.Equal("Ana Pérez", cliente.NombreCompleto);
            },
            cliente =>
            {
                Assert.Equal("987654321", cliente.Rut);
                Assert.Equal("Luis Gómez", cliente.NombreCompleto);
            });
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("En proceso")]
    [InlineData("Listo")]
    [InlineData("Entregado")]
    [InlineData("Cancelado")]
    public async Task Actualizar_AceptaEstadosValidosSinAlterarImporte(string estado)
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Total = 1000 } };
        var resultado = await CrearServicio(repo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = estado });
        Assert.Equal(1, repo.Actualizaciones);
        Assert.Equal(estado, repo.Pedido.Estado);
        Assert.Equal(estado, resultado.Estado);
        Assert.Equal(1000, resultado.Total);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Desconocido")]
    public async Task Actualizar_RechazaEstadoInvalidoSinModificar(string estado)
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { Estado = "Pendiente" } };
        await Assert.ThrowsAsync<ArgumentException>(() => CrearServicio(repo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = estado }));
        Assert.Equal(0, repo.Actualizaciones);
        Assert.Equal("Pendiente", repo.Pedido.Estado);
    }

    [Fact]
    public async Task Actualizar_RechazaPedidoInexistente()
    {
        var repo = new RepositorioPedidos();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CrearServicio(repo).ActualizarEstadoAsync(99, new UpdateEstadoPedidoDto { Estado = "Listo" }));
        Assert.Equal(0, repo.Actualizaciones);
    }

    [Fact]
    public async Task AlPasarAListo_EnviaCorreoAlCliente()
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "En proceso" }, Cliente = new Cliente { Rut = "123456785", Nombre = "Ana", Apellido = "Pérez", Correo = "ana@example.test" } };
        var correo = new CorreoFalso();
        var servicio = CrearServicio(repo, correo);

        var resultado = await servicio.ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.Equal(1, correo.Envios);
        Assert.Equal("Enviada", resultado.EstadoNotificacion);
        Assert.Equal("Listo", repo.Pedido.Estado);
    }

    [Fact]
    public async Task FalloCorreo_MantieneListoEInformaElFallo()
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "En proceso" }, Cliente = new Cliente { Rut = "123456785", Nombre = "Ana", Apellido = "Pérez", Correo = "ana@example.test" } };
        var correo = new CorreoFalso { Resultado = false };
        var servicio = CrearServicio(repo, correo);
        var resultado = await servicio.ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.Equal("Listo", resultado.Estado);
        Assert.Equal("Fallida", resultado.EstadoNotificacion);
    }

    [Fact]
    public async Task EstadoIntermedio_NoIntentaEnviarCorreo()
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "Pendiente" } };
        var correo = new CorreoFalso();

        await CrearServicio(repo, correo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "En proceso" });

        Assert.Equal(0, correo.Envios);
    }

    [Fact]
    public async Task ClienteSinCorreo_DevuelveFalloSinLlamarAlServicioDeCorreo()
    {
        var repo = new RepositorioPedidos { Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "En proceso" } };
        var correo = new CorreoFalso();

        var resultado = await CrearServicio(repo, correo).ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });
        Assert.Equal("Listo", resultado.Estado);
        Assert.Equal("Fallida", resultado.EstadoNotificacion);
        Assert.Equal(0, correo.Envios);
    }

    [Fact]
    public async Task CorreoInvalido_DevuelveFalloSinEnviarNotificacion()
    {
        var repo = new RepositorioPedidos
        {
            Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "En proceso" },
            Cliente = new Cliente
            {
                Rut = "123456785",
                Nombre = "Ana",
                Apellido = "Pérez",
                Correo = "correo-invalido"
            }
        };
        var correo = new CorreoFalso();

        var resultado = await CrearServicio(repo, correo)
            .ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.Equal("Listo", repo.Pedido.Estado);
        Assert.Equal("Fallida", resultado.EstadoNotificacion);
        Assert.Equal(0, correo.Envios);
    }

    [Fact]
    public async Task ExcepcionAlEnviarCorreo_MantieneEstadoListoEInformaFallo()
    {
        var repo = new RepositorioPedidos
        {
            Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "En proceso" },
            Cliente = new Cliente
            {
                Rut = "123456785",
                Nombre = "Ana",
                Apellido = "Pérez",
                Correo = "ana@example.test"
            }
        };
        var correo = new CorreoFalso { Error = new InvalidOperationException("Servicio de correo no disponible.") };

        var resultado = await CrearServicio(repo, correo)
            .ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.Equal("Listo", repo.Pedido.Estado);
        Assert.Equal("Fallida", resultado.EstadoNotificacion);
        Assert.Equal(1, correo.Envios);
    }

    [Fact]
    public async Task PedidoYaListo_NoReenviaCorreo()
    {
        var repo = new RepositorioPedidos
        {
            Pedido = new Pedido { IdPedido = 8, Rut = "123456785", Estado = "Listo" }
        };
        var correo = new CorreoFalso();

        var resultado = await CrearServicio(repo, correo)
            .ActualizarEstadoAsync(8, new UpdateEstadoPedidoDto { Estado = "Listo" });

        Assert.Equal("Listo", resultado.Estado);
        Assert.Null(resultado.EstadoNotificacion);
        Assert.Equal(1, repo.Actualizaciones);
        Assert.Equal(0, correo.Envios);
    }

    private sealed class RepositorioPedidos : IPedidoRepository
    {
        public Cliente? Cliente { get; set; } = new() { Rut = "123456785", Nombre = "Ana", Apellido = "Pérez" };
        public Pedido? Pedido { get; set; }
        public Pedido? Guardado { get; private set; }
        public string? RutConsultado { get; private set; }
        public int Actualizaciones { get; private set; }
        public IEnumerable<PedidoResponseDto> PedidosDetalle { get; set; } = [];
        public PedidoResponseDto? PedidoDetalle { get; set; }
        public IEnumerable<Cliente> ClientesActivos { get; set; } = [];
        public Task<Cliente?> ObtenerClienteActivoPorRutAsync(string rut) { RutConsultado = rut; return Task.FromResult(Cliente); }
        public Task<Cliente?> ObtenerClientePorRutAsync(string rut) => Task.FromResult(Cliente);
        public Task<Pedido> CrearPedidoAsync(Pedido pedido) { Guardado = pedido; pedido.IdPedido = 42; return Task.FromResult(pedido); }
        public Task<Pedido?> ObtenerPedidoPorIdAsync(int id) => Task.FromResult(Pedido);
        public Task ActualizarPedidoAsync(Pedido pedido) { Actualizaciones++; return Task.CompletedTask; }
        public Task<IEnumerable<Pedido>> ObtenerPedidosAsync() => Task.FromResult<IEnumerable<Pedido>>([]);
        public Task<IEnumerable<PedidoResponseDto>> ObtenerPedidosDetalleAsync() => Task.FromResult(PedidosDetalle);
        public Task<PedidoResponseDto?> ObtenerPedidoDetalleAsync(int id) => Task.FromResult(PedidoDetalle);
        public Task<IEnumerable<Cliente>> ObtenerClientesActivosAsync() => Task.FromResult(ClientesActivos);
    }

    private sealed class CorreoFalso : IPedidoCorreoService
    {
        public int Envios { get; private set; }
        public bool Resultado { get; set; } = true;
        public Exception? Error { get; set; }
        public Task<bool> EnviarPedidoListoAsync(string? destinatario, string nombreCliente, int idPedido, CancellationToken cancellationToken)
        {
            Envios++;
            if (Error is not null) throw Error;
            return Task.FromResult(Resultado);
        }
    }
}
