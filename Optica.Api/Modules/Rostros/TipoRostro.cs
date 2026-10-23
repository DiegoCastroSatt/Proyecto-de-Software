using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Optica.Api.Modules.Rostros;

[Table("tipos_rostro")]
public sealed class TipoRostro
{
    [Key, Column("id_tipo_rostro")] public int Id { get; set; }
    [Column("codigo"), MaxLength(80)] public string Codigo { get; set; } = "";
    [Column("nombre"), MaxLength(80)] public string Nombre { get; set; } = "";
    [Column("rasgos"), MaxLength(500)] public string Rasgos { get; set; } = "";
    [Column("consejo"), MaxLength(500)] public string Consejo { get; set; } = "";
    [Column("ruta_imagen"), MaxLength(255)] public string? RutaImagen { get; set; }
    [Column("imagen", TypeName = "mediumblob")] public byte[]? Imagen { get; set; }
    [Column("tipo_imagen"), MaxLength(30)] public string? TipoImagen { get; set; }
    [Column("activo")] public bool Activo { get; set; } = true;
}

public sealed class CrearTipoRostroRequest
{
    public string Nombre { get; set; } = "";
    public string Rasgos { get; set; } = "";
    public string Consejo { get; set; } = "";
    public IFormFile? Imagen { get; set; }
    public int[] Marcos { get; set; } = [];
}

public sealed record TipoRostroResponse(int Id, string Codigo, string Nombre, string Rasgos, string Consejo, bool Activo, string? RutaImagen, int[] Marcos);
public sealed record AsociarMarcosRequest(int[] Marcos);
