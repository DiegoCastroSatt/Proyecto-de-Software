namespace Optica.Api.Modules.Encuestas.Interfaces;

public interface ISolicitudEncuestasService
{
    /// <summary>
    /// Crea la encuesta y programa su correo para las atenciones realizadas recientes que aún no tienen una.
    /// Devuelve la cantidad de encuestas creadas.
    /// </summary>
    Task<int> ProgramarPendientes(CancellationToken cancellationToken);
}
