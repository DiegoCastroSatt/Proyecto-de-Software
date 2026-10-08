public interface IRecetaService
{
    Task<RecetaResponseDto> CrearReceta(CrearRecetaDto dto);
    Task<RecetaResponseDto> ObtenerReceta(int id);
    Task<RecetaResponseDto> ActualizarReceta(int id, CrearRecetaDto dto);
    Task<List<ClienteSugerenciaDto>> BuscarSugerenciasRut(string termino);
    Task<List<RecetaHistorialDto>> ObtenerHistorialPorRut(string rut);
}