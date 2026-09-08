using Optica.Api.Modules.AgendaReservas.Models;

namespace Optica.Api.RegistroAgenda.Tests;

public class RegistroAgendaTests
{
    [Fact]
    public async Task RegistrarHora_ConClienteYHoraDisponible_GuardaLaReservaEnLaAgenda()
    {
        var repositorio = new MemoriaReservaRepository();
        var servicio = new global::ReservaService(repositorio);
        var fecha = DateTime.Today.AddDays(1);

        var resultado = await servicio.CrearReserva(CrearSolicitud(fecha, new TimeSpan(14, 0, 0)));

        var reservaGuardada = await repositorio.ObtenerPorId(resultado.Id);
        Assert.NotNull(reservaGuardada);
        Assert.Equal(fecha.Date, reservaGuardada!.Fecha.Date);
        Assert.Equal(new TimeSpan(14, 0, 0), reservaGuardada.Hora);
        Assert.Equal("Pendiente", reservaGuardada.Estado);
    }

    [Fact]
    public async Task RegistrarHora_ConHorarioOcupado_IndicaQueLaHoraYaEstaReservada()
    {
        var repositorio = new MemoriaReservaRepository();
        var servicio = new global::ReservaService(repositorio);
        var fecha = DateTime.Today.AddDays(1);
        var hora = new TimeSpan(15, 0, 0);
        await servicio.CrearReserva(CrearSolicitud(fecha, hora));

        var excepcion = await Assert.ThrowsAsync<Exception>(
            () => servicio.CrearReserva(CrearSolicitud(fecha, hora)));

        Assert.Equal("La hora seleccionada ya está reservada.", excepcion.Message);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_DiferenciaUnaHoraOcupadaDeUnaHoraDisponible()
    {
        var repositorio = new MemoriaReservaRepository();
        var fecha = DateTime.Today.AddDays(1);
        await repositorio.Crear(new Reserva
        {
            ClienteId = 1,
            Fecha = fecha,
            Hora = new TimeSpan(13, 0, 0),
            Estado = "Pendiente"
        });

        var horaOcupada = await repositorio.ExisteReserva(fecha, new TimeSpan(13, 0, 0));
        var horaDisponible = await repositorio.ExisteReserva(fecha, new TimeSpan(16, 0, 0));

        Assert.True(horaOcupada);
        Assert.False(horaDisponible);
    }

    [Fact]
    public async Task RegistrarHora_ConFechaPasada_RechazaLaSolicitud()
    {
        var servicio = new global::ReservaService(new MemoriaReservaRepository());

        var excepcion = await Assert.ThrowsAsync<Exception>(
            () => servicio.CrearReserva(CrearSolicitud(DateTime.Today.AddDays(-1), new TimeSpan(14, 0, 0))));

        Assert.Equal("No se puede reservar una fecha pasada.", excepcion.Message);
    }

    private static global::CrearReservaDto CrearSolicitud(DateTime fecha, TimeSpan hora) => new()
    {
        NombreCompleto = "Cliente de prueba",
        Rut = "12.345.678-9",
        Telefono = "+56 9 1234 5678",
        Correo = "cliente@ejemplo.cl",
        Fecha = fecha,
        Hora = hora
    };
}
