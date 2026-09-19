using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Services;

public class CalculoVenta : ICalculoVenta
{
    public void Calcular(Venta venta)
    {
        foreach (var detalle in venta.Detalles)
            detalle.Subtotal = detalle.PrecioUnitario * detalle.Cantidad;
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);
        if (venta.Total > 99999999.99m)
            throw new ArgumentException("El total de la venta supera el máximo permitido.");
    }
}
