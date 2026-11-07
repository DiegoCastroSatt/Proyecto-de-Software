using System.ComponentModel.DataAnnotations;

namespace Optica.Api.Modules.Resenas.DTOs;

public sealed class CrearResenaDto
{
    [Range(1, 5)]
    public int Calificacion { get; set; }

    [Required, StringLength(1000, MinimumLength = 3)]
    public string Comentario { get; set; } = string.Empty;
}
