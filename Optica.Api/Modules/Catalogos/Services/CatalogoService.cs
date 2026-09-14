using Optica.Api.Modules.Catalogos.Models;

public class CatalogoService : ICatalogoService
{
    private static readonly string[] TiposValidos = ["Marca", "Color", "Categoria"];
    private readonly ICatalogoRepository _catalogoRepository;

    public CatalogoService(ICatalogoRepository catalogoRepository)
    {
        _catalogoRepository = catalogoRepository;
    }

    public async Task<IReadOnlyList<CatalogoItemResponseDto>> ObtenerPorTipo(string tipo)
    {
        var tipoNormalizado = NormalizarTipo(tipo);
        var items = await _catalogoRepository.ObtenerPorTipo(tipoNormalizado);
        return items.Select(Mapear).ToList();
    }

    public async Task<CatalogoItemResponseDto> Crear(CrearCatalogoItemDto dto)
    {
        var tipo = NormalizarTipo(dto.Tipo);
        var nombre = dto.Nombre.Trim();

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 60)
        {
            throw new ArgumentException("El nombre debe tener entre 1 y 60 caracteres.");
        }

        var existente = await _catalogoRepository.ObtenerPorNombre(tipo, nombre);
        if (existente is not null)
        {
            return Mapear(existente);
        }

        return Mapear(await _catalogoRepository.Crear(new CatalogoItem
        {
            Tipo = tipo,
            Nombre = nombre
        }));
    }

    private static string NormalizarTipo(string tipo)
    {
        var tipoNormalizado = TiposValidos.FirstOrDefault(tipoValido =>
            string.Equals(tipoValido, tipo.Trim(), StringComparison.OrdinalIgnoreCase));

        return tipoNormalizado ?? throw new ArgumentException("El tipo de catálogo no es válido.");
    }

    private static CatalogoItemResponseDto Mapear(CatalogoItem item) => new()
    {
        Id = item.IdCatalogo,
        Tipo = item.Tipo,
        Nombre = item.Nombre
    };
}
