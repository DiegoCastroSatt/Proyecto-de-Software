namespace Optica.Api.Modules.AgendaReservas.Services;

public static class HoraChile
{
    private static readonly TimeZoneInfo ZonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    public static DateTimeOffset ObtenerAhora(TimeProvider proveedorTiempo) =>
        TimeZoneInfo.ConvertTime(proveedorTiempo.GetUtcNow(), ZonaHoraria);

    /// <summary>Convierte una fecha y hora locales de Chile a UTC.</summary>
    public static DateTime ConvertirAUtc(DateTime fecha, TimeSpan hora) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fecha.Date.Add(hora), DateTimeKind.Unspecified), ZonaHoraria);
}
