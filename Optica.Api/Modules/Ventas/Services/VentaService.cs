using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Services;

public class VentaService(IVentaRepository repositorio, IConsultaProductoVenta productos,
    ICalculoVenta calculo, TimeProvider reloj) : IVentaService
{
    private static readonly TimeZoneInfo ZonaHoraria = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

    public Task<VentasPaginaDto> ListarPagina(int desplazamiento, int tamano) => repositorio.ListarPagina(desplazamiento, tamano);
    public Task<VentasResumenDto> Resumir(DateTime desde, DateTime hasta, DateTime desdeRanking, DateTime hastaRanking, int limite)
        => repositorio.Resumir(desde, hasta, desdeRanking, hastaRanking, limite);

    public Task<IReadOnlyList<VentaResponseDto>> Listar() => repositorio.Listar();

    public async Task<VentaCreadaResponseDto> Crear(CrearVentaDto dto, int idResponsable)
    {
        if (idResponsable <= 0)
            throw new ArgumentException("No se pudo identificar al responsable de la venta.");

        if (dto.Productos is null || dto.Productos.Count == 0 ||
            dto.Productos.Any(p => p is null || string.IsNullOrWhiteSpace(p.CodigoProducto) || p.Cantidad <= 0))
            throw new ArgumentException("Agrega al menos un código válido con cantidad mayor que cero.");

        int? clienteId = null;
        if (!string.IsNullOrWhiteSpace(dto.RutCliente))
        {
            var rut = Optica.Api.Modules.Clientes.Services.RutChilenoValidator.Normalizar(dto.RutCliente);
            clienteId = await repositorio.BuscarClienteActivo(rut)
                ?? throw new ArgumentException("No se encontró un cliente activo con ese RUT. Selecciona uno de las sugerencias o deja el campo vacío.");
        }
        var venta = new Venta { ClienteId = clienteId, Fecha = TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), ZonaHoraria).DateTime };
        foreach (var item in dto.Productos)
        {
            var codigo = item.CodigoProducto.Trim();
            var producto = await productos.Buscar(codigo)
                ?? throw new ArgumentException($"No se encontró el producto con código {codigo}.");
            var detalle = venta.Detalles.SingleOrDefault(d => d.ProductoId == producto.IdProducto);
            if (detalle is null)
            {
                detalle = new DetalleVenta
                {
                    ProductoId = producto.IdProducto,
                    NombreProducto = producto.Nombre,
                    PrecioUnitario = producto.Precio
                };
                venta.Detalles.Add(detalle);
            }
            if ((long)detalle.Cantidad + item.Cantidad > int.MaxValue)
                throw new ArgumentException("La cantidad del producto es demasiado grande.");
            if (producto.Stock <= 0)
                throw new ArgumentException($"{producto.Nombre}: Sin productos en stock.");
            if ((long)detalle.Cantidad + item.Cantidad > producto.Stock)
                throw new ArgumentException($"{producto.Nombre}: stock insuficiente. Disponibles: {producto.Stock}.");
            detalle.Cantidad += item.Cantidad;
        }
        calculo.Calcular(venta);
        await repositorio.Guardar(venta, idResponsable);
        return new VentaCreadaResponseDto(venta.IdVenta, venta.Fecha, venta.Total,
            venta.Detalles.Select(d => new DetalleVentaResponseDto(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList());
    }
}
