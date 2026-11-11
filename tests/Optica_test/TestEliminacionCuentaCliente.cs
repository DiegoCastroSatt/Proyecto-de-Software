using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Encuestas.Models;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Productos.Models;
using Optica.Api.Modules.RegistroRecetas.Models;
using Optica.Api.Modules.Resenas.Models;
using Optica.Api.Modules.Resenas.Repositories;
using Optica.Ventas.Tests;
using Xunit;

namespace Optica.Tests;

// Pruebas de la eliminación de cuenta de cliente con anonimización de la ficha.
public sealed class TestEliminacionCuentaCliente : IDisposable
{
    private const string Contrasena = "Optica123!";
    private const string RutCliente = "12345678-5";

    private readonly OpticaDbContext db = new(new DbContextOptionsBuilder<OpticaDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private readonly EntornoPrueba entorno = new()
    {
        WebRootPath = Path.Combine(Path.GetTempPath(), "optica-tests-eliminacion", Guid.NewGuid().ToString())
    };

    private readonly PasswordHasher<CuentaCliente> hasher = new();

    private EliminacionCuentaClienteService Servicio() => new(
        db, hasher, entorno, TimeProvider.System, NullLogger<EliminacionCuentaClienteService>.Instance);

    private async Task<Cliente> CrearClienteConCuentaAsync()
    {
        var cliente = new Cliente
        {
            Rut = RutCliente, Nombre = "Ana", Apellido = "Pérez",
            Telefono = "912345678", Correo = "ana@example.test", Estado = "Activo"
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        var cuenta = new CuentaCliente
        {
            IdCliente = cliente.IdCliente, Correo = "ana@example.test",
            CorreoNormalizado = "ANA@EXAMPLE.TEST", Estado = "Activa"
        };
        cuenta.ContrasenaHash = hasher.HashPassword(cuenta, Contrasena);
        db.CuentasCliente.Add(cuenta);
        await db.SaveChangesAsync();
        return cliente;
    }

    [Fact]
    public async Task Eliminar_RechazaContrasenaIncorrectaSinCambiarDatos()
    {
        var cliente = await CrearClienteConCuentaAsync();

        var resultado = await Servicio().EliminarAsync(cliente.IdCliente, "Otra123!", CancellationToken.None);

        Assert.Equal(ResultadoEliminacionCuenta.ContrasenaIncorrecta, resultado);
        Assert.Single(await db.CuentasCliente.ToListAsync());
        Assert.Equal("Ana", (await db.Clientes.SingleAsync()).Nombre);
    }

    [Fact]
    public async Task Eliminar_SinCuentaActivaDevuelveSinCuenta()
    {
        var resultado = await Servicio().EliminarAsync(999, Contrasena, CancellationToken.None);

        Assert.Equal(ResultadoEliminacionCuenta.SinCuenta, resultado);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("En proceso")]
    [InlineData("Listo")]
    public async Task Eliminar_BloqueaSiHayPedidosEnCurso(string estado)
    {
        var cliente = await CrearClienteConCuentaAsync();
        db.Pedidos.Add(new Pedido { Rut = RutCliente, Estado = estado, Total = 1000 });
        await db.SaveChangesAsync();

        var resultado = await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        Assert.Equal(ResultadoEliminacionCuenta.PedidosEnCurso, resultado);
        Assert.Single(await db.CuentasCliente.ToListAsync());
        Assert.Equal(RutCliente, (await db.Clientes.SingleAsync()).Rut);
    }

    [Fact]
    public async Task Eliminar_BorraCuentaYAnonimizaFicha()
    {
        var cliente = await CrearClienteConCuentaAsync();

        var resultado = await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        Assert.Equal(ResultadoEliminacionCuenta.Eliminada, resultado);
        Assert.Empty(await db.CuentasCliente.ToListAsync());
        var ficha = await db.Clientes.SingleAsync();
        Assert.Equal(cliente.IdCliente, ficha.IdCliente);
        Assert.Equal("Cliente", ficha.Nombre);
        Assert.Equal("Eliminado", ficha.Apellido);
        Assert.Equal($"ANON-{cliente.IdCliente}", ficha.Rut);
        Assert.Null(ficha.Telefono);
        Assert.Null(ficha.Correo);
        Assert.Equal("Inactivo", ficha.Estado);
    }

    [Fact]
    public async Task Eliminar_ConservaPedidosConRutAnonimoYSinAnotaciones()
    {
        var cliente = await CrearClienteConCuentaAsync();
        var pedido = new Pedido { Rut = RutCliente, Estado = "Entregado", Total = 45990, Anotaciones = "Llamar a Ana al 912345678" };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        var conservado = await db.Pedidos.SingleAsync();
        Assert.Equal($"ANON-{cliente.IdCliente}", conservado.Rut);
        Assert.Equal("Entregado", conservado.Estado);
        Assert.Equal(45990, conservado.Total);
        Assert.Null(conservado.Anotaciones);
    }

    [Fact]
    public async Task Eliminar_EliminaCorreosDePedidosYReservasDelCliente()
    {
        var cliente = await CrearClienteConCuentaAsync();
        var pedido = new Pedido { Rut = RutCliente, Estado = "Entregado" };
        var horario = new Horario { Fecha = DateTime.Today.AddDays(-3), HoraInicio = TimeSpan.FromHours(10), HoraFin = TimeSpan.FromHours(11), Estado = "Habilitada" };
        db.Pedidos.Add(pedido);
        db.Horarios.Add(horario);
        await db.SaveChangesAsync();
        var reserva = new Reserva { ClienteId = cliente.IdCliente, HorarioId = horario.Id, Estado = "Realizada" };
        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        db.CorreosPedido.Add(new CorreoPedidoPendiente { PedidoId = pedido.IdPedido, Destinatario = "ana@example.test", Asunto = "Pedido", CuerpoHtml = "<p>Ana</p>" });
        db.CorreosPedido.Add(new CorreoPedidoPendiente { PedidoId = pedido.IdPedido + 100, Destinatario = "otro@example.test", Asunto = "Otro", CuerpoHtml = "<p>Otro</p>" });
        db.CorreosReserva.Add(new CorreoReservaPendiente { ReservaId = reserva.Id, HorarioId = horario.Id, Tipo = "Confirmacion", Destinatario = "ana@example.test", Asunto = "Reserva", CuerpoHtmlProtegido = "x" });
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        Assert.Equal("otro@example.test", (await db.CorreosPedido.SingleAsync()).Destinatario);
        Assert.Empty(await db.CorreosReserva.ToListAsync());
    }

    [Fact]
    public async Task Eliminar_AnulaEncuestasPendientesYConservaRespuestasAnonimas()
    {
        var cliente = await CrearClienteConCuentaAsync();
        var horario = new Horario { Fecha = DateTime.Today.AddDays(-1), HoraInicio = TimeSpan.FromHours(10), HoraFin = TimeSpan.FromHours(11), Estado = "Habilitada" };
        db.Horarios.Add(horario);
        await db.SaveChangesAsync();
        var pendiente = new Reserva { ClienteId = cliente.IdCliente, HorarioId = horario.Id, Estado = "Realizada" };
        var respondida = new Reserva { ClienteId = cliente.IdCliente, HorarioId = horario.Id, Estado = "Realizada" };
        db.Reservas.AddRange(pendiente, respondida);
        await db.SaveChangesAsync();
        db.EncuestasAtencion.AddRange(
            new EncuestaAtencion { ReservaId = pendiente.Id, TokenHash = new string('a', 64), TokenExpiraUtc = DateTime.UtcNow.AddDays(10), FechaEnvioUtc = DateTime.UtcNow },
            new EncuestaAtencion { ReservaId = respondida.Id, FechaEnvioUtc = DateTime.UtcNow, Respondida = true });
        db.RespuestasEncuestaAtencion.Add(new RespuestaEncuestaAtencion
        {
            CalificacionGeneral = 5, CalificacionTrato = 5, CalificacionPuntualidad = 4,
            Comentario = "Muy bien", FechaRespuesta = DateTime.Today
        });
        db.CorreosReserva.Add(new CorreoReservaPendiente { ReservaId = pendiente.Id, HorarioId = horario.Id, Tipo = "Encuesta", Destinatario = "ana@example.test", Asunto = "Encuesta", CuerpoHtmlProtegido = "x" });
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        var encuesta = await db.EncuestasAtencion.SingleAsync(e => e.ReservaId == pendiente.Id);
        Assert.Null(encuesta.TokenHash);
        Assert.Null(encuesta.TokenExpiraUtc);
        Assert.False(encuesta.Respondida);
        Assert.Empty(await db.CorreosReserva.ToListAsync());
        Assert.Equal("Muy bien", (await db.RespuestasEncuestaAtencion.SingleAsync()).Comentario);
    }

    [Fact]
    public async Task Eliminar_LimpiaMotivoYCancelaSoloReservasFuturasActivas()
    {
        var cliente = await CrearClienteConCuentaAsync();
        var futuro = new Horario { Fecha = DateTime.Today.AddDays(5), HoraInicio = TimeSpan.FromHours(10), HoraFin = TimeSpan.FromHours(11), Estado = "Habilitada" };
        var pasado = new Horario { Fecha = DateTime.Today.AddDays(-5), HoraInicio = TimeSpan.FromHours(10), HoraFin = TimeSpan.FromHours(11), Estado = "Habilitada" };
        db.Horarios.AddRange(futuro, pasado);
        await db.SaveChangesAsync();
        var reservaFutura = new Reserva { ClienteId = cliente.IdCliente, HorarioId = futuro.Id, Estado = "Confirmada", Motivo = "Control de Ana", TokenCancelacionHash = new string('a', 64) };
        var reservaPasada = new Reserva { ClienteId = cliente.IdCliente, HorarioId = pasado.Id, Estado = "Realizada", Motivo = "Dolor de cabeza" };
        db.Reservas.AddRange(reservaFutura, reservaPasada);
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        var futura = await db.Reservas.SingleAsync(r => r.Id == reservaFutura.Id);
        var pasada = await db.Reservas.SingleAsync(r => r.Id == reservaPasada.Id);
        Assert.Equal("Cancelada", futura.Estado);
        Assert.Null(futura.Motivo);
        Assert.Null(futura.TokenCancelacionHash);
        Assert.Equal("Realizada", pasada.Estado);
        Assert.Null(pasada.Motivo);
    }

    [Fact]
    public async Task Eliminar_ConservaRecetasYBorraImagenesSubidas()
    {
        var cliente = await CrearClienteConCuentaAsync();
        var carpeta = Path.Combine(entorno.WebRootPath, "uploads", "recetas");
        Directory.CreateDirectory(carpeta);
        var archivo = Path.Combine(carpeta, "receta-ana.png");
        await File.WriteAllBytesAsync(archivo, [1, 2, 3]);
        db.Recetas.Add(new Receta
        {
            ClienteId = cliente.IdCliente, Observaciones = "Miopía", ImagenPath = "/uploads/recetas/receta-ana.png",
            Graduaciones = [new Graduacion { Ojo = "OD", Esfera = -1.25m }]
        });
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        var receta = await db.Recetas.Include(r => r.Graduaciones).SingleAsync();
        Assert.Equal(cliente.IdCliente, receta.ClienteId);
        Assert.Null(receta.ImagenPath);
        Assert.Single(receta.Graduaciones);
        Assert.False(File.Exists(archivo));
    }

    [Fact]
    public async Task Eliminar_ReseñasSeMuestranComoClienteAnonimo()
    {
        var cliente = await CrearClienteConCuentaAsync();
        db.Productos.Add(new Producto { IdProducto = 10, Codigo = "OPT-10", Nombre = "Armazón", Categoria = "Armazones" });
        db.Resenas.Add(new Resena { IdCliente = cliente.IdCliente, IdProducto = 10, Calificacion = 5, Comentario = "Muy bueno", FechaCreacionUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await Servicio().EliminarAsync(cliente.IdCliente, Contrasena, CancellationToken.None);

        var resenas = await new ResenaRepository(db).ListarPorProducto(10, CancellationToken.None);
        Assert.Equal("Cliente anónimo", Assert.Single(resenas).NombreCliente);
        Assert.Equal("Muy bueno", resenas[0].Comentario);
    }

    public void Dispose()
    {
        db.Dispose();
        if (Directory.Exists(entorno.WebRootPath))
            Directory.Delete(entorno.WebRootPath, recursive: true);
    }
}
