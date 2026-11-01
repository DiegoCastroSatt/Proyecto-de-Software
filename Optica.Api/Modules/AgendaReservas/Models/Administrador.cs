using System.ComponentModel.DataAnnotations.Schema;
using Optica.Api.Modules.Accesos;

namespace Optica.Api.Modules.AgendaReservas.Models;

// Conserva el contrato del dominio y las rutas existentes sobre la tabla única de personal.
public class Administrador : Trabajador
{
    [NotMapped]
    public int IdAdministrador { get => IdTrabajador; set => IdTrabajador = value; }
}
