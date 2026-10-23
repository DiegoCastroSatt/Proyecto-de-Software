using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Optica.Api.Modules.Marcos;

[Table("tipos_marco")]
public sealed class TipoMarco
{
    [Key, Column("id_tipo_marco")] public int Id { get; set; }
    [Column("codigo"), MaxLength(80)] public string Codigo { get; set; } = "";
    [Column("nombre"), MaxLength(80)] public string Nombre { get; set; } = "";
    [Column("categoria"), MaxLength(50)] public string Categoria { get; set; } = "";
    [Column("svg", TypeName = "mediumtext")] public string Svg { get; set; } = "";
    [Column("rostros", TypeName = "text")] public string Rostros { get; set; } = "";
    [Column("activo")] public bool Activo { get; set; } = true;
}

public sealed record CrearTipoMarcoRequest(string Nombre, string Categoria, string Svg, string[] Rostros);
public sealed record EstadoTipoMarcoRequest(bool Activo);
public sealed record TipoMarcoResponse(int Id, string Codigo, string Nombre, string Categoria, bool Activo, string[] Rostros, int Stock, int ProductosVinculados = 0);

public sealed record ValidarDisenoMarcoRequest(string Svg);

public sealed record VincularProductosMarcoRequest(int[] Productos);

public sealed record ProductoVinculableMarcoResponse(int Id, string Codigo, string Nombre, string Categoria, string Marca, int Stock, string Estado, int? IdTipoMarco);

[Table("formas_lente")]
public sealed class FormaLente
{
    [Key, Column("id_forma_lente")] public int Id { get; set; }
    [Column("codigo"), MaxLength(50)] public string Codigo { get; set; } = "";
    [Column("nombre"), MaxLength(80)] public string Nombre { get; set; } = "";
    [Column("nombre_normalizado"), MaxLength(80)] public string NombreNormalizado { get; set; } = "";
    [Column("geometria"), MaxLength(20)] public string Geometria { get; set; } = "contorno";
    [Column("mantener_proporcion")] public bool MantenerProporcion { get; set; }
    [Column("contorno", TypeName = "text")] public string Contorno { get; set; } = "";
}
public sealed record FormaLenteResponse(string Id, string Nombre, string Contorno, string Geometria = "contorno", bool MantenerProporcion = false);
