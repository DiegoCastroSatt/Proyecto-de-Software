using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Ventas.Services;

var repositorio = new RepositorioPrueba();
var servicio = new VentaService(repositorio, new ConsultaPrueba(), new CalculoVenta(), TimeProvider.System);
CrearVentaDto Solicitud(params (string Codigo, int Cantidad)[] items) => new()
{
    Productos = items.Select(i => new ProductoVentaDto { CodigoProducto = i.Codigo, Cantidad = i.Cantidad }).ToList()
};
void Verificar(bool condicion, string mensaje)
{
    if (!condicion) throw new Exception(mensaje);
}
var venta = await servicio.Crear(Solicitud(("A", 2), ("B", 1), ("A", 3)));
Verificar(venta.Total == 750 && venta.Productos.Count == 2, "Total o agrupación incorrectos.");
Verificar(venta.Productos.Single(p => p.ProductoId == 1).Cantidad == 5, "No acumuló repetidos.");
Verificar(venta.IdVenta == 1 && repositorio.Guardadas == 1, "No devolvió la ID persistida.");
foreach (var invalida in new[] { Solicitud(), Solicitud(("A", 0)), Solicitud(("A", 1), ("X", 1)),
    Solicitud(("A", int.MaxValue), ("A", 1)), Solicitud(("B", 1000000)) })
{
    try { await servicio.Crear(invalida); throw new Exception("Aceptó una venta inválida."); }
    catch (ArgumentException) { }
    Verificar(repositorio.Guardadas == 1, "Guardó una venta inválida o parcial.");
}
var alternativo = new VentaService(repositorio, new ConsultaPrueba(), new CalculoAlternativo(), TimeProvider.System);
var otra = await alternativo.Crear(Solicitud(("A", 1)));
Verificar(otra.Total == 50, "No permite sustituir el cálculo sin modificar el servicio.");
Console.WriteLine("Correcto: agrupación, totales, persistencia, validaciones y sustitución del cálculo.");

sealed class RepositorioPrueba : IVentaRepository
{
    public int Guardadas { get; private set; }
    public Task Guardar(Venta venta) { venta.IdVenta = ++Guardadas; return Task.CompletedTask; }
    public Task<IReadOnlyList<VentaResponseDto>> Listar() => Task.FromResult<IReadOnlyList<VentaResponseDto>>([]);
}
sealed class ConsultaPrueba : IConsultaProductoVenta
{
    public Task<ProductoCajaDto?> Buscar(string codigo) => Task.FromResult(codigo switch
    {
        "A" => new ProductoCajaDto(1, "A", "Producto A", 100),
        "B" => new ProductoCajaDto(2, "B", "Producto B", 250),
        _ => null
    });
}
sealed class CalculoAlternativo : ICalculoVenta
{
    public void Calcular(Venta venta)
    {
        foreach (var detalle in venta.Detalles) detalle.Subtotal = detalle.PrecioUnitario * detalle.Cantidad / 2;
        venta.Total = venta.Detalles.Sum(d => d.Subtotal);
    }
}
