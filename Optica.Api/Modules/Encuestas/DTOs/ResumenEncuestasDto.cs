namespace Optica.Api.Modules.Encuestas.DTOs;

public sealed record DistribucionEstrellasDto(int Estrellas, int Cantidad);

public sealed record EvolucionMensualEncuestasDto(
    string Mes,
    int Respuestas,
    double PromedioGeneral,
    double PromedioTrato,
    double PromedioPuntualidad);

public sealed record ResumenEncuestasDto(
    int Enviadas,
    int Respondidas,
    double TasaRespuesta,
    double? PromedioGeneral,
    double? PromedioTrato,
    double? PromedioPuntualidad,
    IReadOnlyList<DistribucionEstrellasDto> DistribucionGeneral,
    IReadOnlyList<EvolucionMensualEncuestasDto> EvolucionMensual);
