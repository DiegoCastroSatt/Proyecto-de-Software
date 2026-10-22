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
