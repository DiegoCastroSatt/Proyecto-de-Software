namespace Optica.Api.Modules.AgendaReservas.Models;

public enum TipoResultadoAccionReserva
{
    TokenInvalido,
    TokenExpirado,
    Confirmada,
    YaConfirmada,
    Cancelada,
    YaCancelada,
    NoVigente
}

public sealed class ResultadoAccionReserva
{
    public TipoResultadoAccionReserva Tipo { get; init; }
    public DateTime? Fecha { get; init; }
    public TimeSpan? HoraInicio { get; init; }
}
