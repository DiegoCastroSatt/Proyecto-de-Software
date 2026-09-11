using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Optica.Api.Modules.Clientes.Models;

[Table("clientes")]
public class Cliente
{
    [Key]
    [Column("id_cliente")]
    public int IdCliente { get; set; }

    [Column("rut")]
    [Required]
    [MaxLength(12)]
    public string Rut { get; set; } = string.Empty;

    [Column("nombre")]
    [Required]
    [MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Column("apellido")]
    [Required]
    [MaxLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Column("telefono")]
    [MaxLength(20)]
    public string? Telefono { get; set; }

    [Column("correo")]
    [MaxLength(100)]
    public string? Correo { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "Activo";

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}