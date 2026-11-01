namespace Optica.Api.Modules.Autenticacion.DTOs;

public sealed record ClienteSesionResponse(int IdCliente, string Nombre, string Correo)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string VersionCredencial { get; init; } = string.Empty;
}
