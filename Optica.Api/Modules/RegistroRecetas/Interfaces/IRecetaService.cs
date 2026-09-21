public interface IRecetaService
{
    Task<RecetaResponseDto> CrearReceta(CrearRecetaDto dto);
}