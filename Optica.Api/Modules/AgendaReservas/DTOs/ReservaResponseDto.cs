using System.Text.Json.Serialization;

public class ReservaResponseDto
{
    public int Id { get; set; }

    public int IdHorario { get; set; }

    public DateTime Fecha { get; set; }

    public TimeSpan Hora { get; set; }

    public string Estado { get; set; } = string.Empty;

    [JsonIgnore]
    public bool CorreoEnviado { get; set; }
}
