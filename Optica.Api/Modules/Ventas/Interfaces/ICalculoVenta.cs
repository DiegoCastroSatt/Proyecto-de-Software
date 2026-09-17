using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface ICalculoVenta
{
    void Calcular(Venta venta);
}
