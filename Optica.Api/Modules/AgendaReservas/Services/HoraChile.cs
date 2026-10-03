namespace Optica.Api.Modules.AgendaReservas.Services;

public static class HoraChile
{
    private static readonly TimeZoneInfo ZonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    public static DateTimeOffset ObtenerAhora(TimeProvider proveedorTiempo) =>
        TimeZoneInfo.ConvertTime(proveedorTiempo.GetUtcNow(), ZonaHoraria);
}
