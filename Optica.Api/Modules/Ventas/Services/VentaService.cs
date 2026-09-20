using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Services;

public class VentaService(IVentaRepository repositorio, IConsultaProductoVenta productos,
    ICalculoVenta calculo, TimeProvider reloj) : IVentaService
{
    public Task<IReadOnlyList<VentaResponseDto>> Listar() => repositorio.Listar();

    public async Task<VentaCreadaResponseDto> Crear(CrearVentaDto dto)
    {
        if (dto.Productos is null || dto.Productos.Count == 0 ||
            dto.Productos.Any(p => p is null || string.IsNullOrWhiteSpace(p.CodigoProducto) || p.Cantidad <= 0))
            throw new ArgumentException("Agrega al menos un código válido con cantidad mayor que cero.");

        var venta = new Venta { Fecha = reloj.GetLocalNow().DateTime };
        foreach (var item in dto.Productos)
        {
            var codigo = item.CodigoProducto.Trim();
            var producto = await productos.Buscar(codigo)
                ?? throw new ArgumentException($"No se encontró el producto con código {codigo}.");
            var detalle = venta.Detalles.SingleOrDefault(d => d.ProductoId == producto.IdProducto);
            if (detalle is null)
            {
                detalle = new DetalleVenta { ProductoId = producto.IdProducto, PrecioUnitario = producto.Precio };
                venta.Detalles.Add(detalle);
            }
            if ((long)detalle.Cantidad + item.Cantidad > int.MaxValue)
                throw new ArgumentException("La cantidad del producto es demasiado grande.");
            detalle.Cantidad += item.Cantidad;
        }
        calculo.Calcular(venta);
        await repositorio.Guardar(venta);
        return new VentaCreadaResponseDto(venta.IdVenta, venta.Fecha, venta.Total,
            venta.Detalles.Select(d => new DetalleVentaResponseDto(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList());
    }
}
