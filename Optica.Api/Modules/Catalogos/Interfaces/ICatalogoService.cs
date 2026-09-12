public interface ICatalogoService
{
    Task<IReadOnlyList<CatalogoItemResponseDto>> ObtenerPorTipo(string tipo);
    Task<CatalogoItemResponseDto> Crear(CrearCatalogoItemDto dto);
}
