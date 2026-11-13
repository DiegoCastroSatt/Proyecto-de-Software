using Optica.Api.Modules.Ventas.DTOs;
using Optica.Api.Modules.Ventas.Models;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface IVentaRepository
{
    Task<IReadOnlyList<VentaResponseDto>> Listar();
    Task<VentasPaginaDto> ListarPagina(int desplazamiento, int tamano) => throw new NotSupportedException();
    Task<VentasResumenDto> Resumir(DateTime desde, DateTime hasta, DateTime desdeRanking, DateTime hastaRanking, int limite)
        => throw new NotSupportedException();
    Task Guardar(Venta venta, int idResponsable);
    Task<int?> BuscarClienteActivo(string rutNormalizado);
}
