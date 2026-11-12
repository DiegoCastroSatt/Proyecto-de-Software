using Optica.Api.Modules.Ventas.DTOs;
namespace Optica.Api.Modules.Ventas.Interfaces;

public interface IVentaService
{
    Task<IReadOnlyList<VentaResponseDto>> Listar();
    Task<VentasPaginaDto> ListarPagina(int desplazamiento, int tamano) => throw new NotSupportedException();
    Task<VentasResumenDto> Resumir(DateTime desde, DateTime hasta, DateTime desdeRanking, DateTime hastaRanking, int limite)
        => throw new NotSupportedException();
    Task<VentaCreadaResponseDto> Crear(CrearVentaDto dto, int idResponsable);
}
