namespace Optica.Api.Modules.Notificaciones.Services;

public static class PoliticaReintentosCorreo
{
    public static TimeSpan ObtenerPausa(int proximoNumeroIntento)
    {
        var exponente = Math.Clamp(proximoNumeroIntento - 1, 0, 10);
        return TimeSpan.FromMinutes(Math.Min(360, Math.Pow(2, exponente)));
    }
}
