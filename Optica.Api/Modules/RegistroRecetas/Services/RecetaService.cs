using System.Text.Json;
using Optica.Api.Modules.RegistroRecetas.Models;

public class RecetaService : IRecetaService
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TamanoMaximoImagen = 5 * 1024 * 1024; // 5 MB

    public RecetaService(IRecetaRepository recetaRepository, IWebHostEnvironment webHostEnvironment)
    {
        _recetaRepository = recetaRepository;
        _webHostEnvironment = webHostEnvironment;
    }

    public Task<RecetaResponseDto> CrearReceta(CrearRecetaDto dto) => GuardarReceta(dto, null);

    public Task<RecetaResponseDto> ActualizarReceta(int id, CrearRecetaDto dto) => GuardarReceta(dto, id);

    public async Task<RecetaResponseDto> ObtenerReceta(int id)
    {
        var receta = await _recetaRepository.ObtenerPorId(id)
            ?? throw new KeyNotFoundException("La receta seleccionada no existe.");
        var cliente = await _recetaRepository.ObtenerClientePorId(receta.ClienteId);
        return MapearReceta(receta, cliente?.Rut ?? string.Empty);
    }

    private async Task<RecetaResponseDto> GuardarReceta(CrearRecetaDto dto, int? id)
    {
        var existente = id.HasValue
            ? await _recetaRepository.ObtenerPorId(id.Value)
                ?? throw new KeyNotFoundException("La receta seleccionada no existe.")
            : null;
        var cliente = await _recetaRepository.BuscarClientePorRut(dto.Rut);
        if (cliente == null)
        {
            throw new ArgumentException("No existe un cliente con ese RUT.");
        }

        var tieneGraduaciones = !string.IsNullOrWhiteSpace(dto.GraduacionesJson);
        var tieneImagen = dto.Imagen != null;

        if (!tieneGraduaciones && !tieneImagen && string.IsNullOrEmpty(existente?.ImagenPath) && !(existente?.Graduaciones.Any() ?? false))
        {
            throw new ArgumentException("Debe registrar la receta escrita o mediante una imagen.");
        }

        var receta = new Receta
        {
            ClienteId = cliente.IdCliente,
            Fecha = dto.Fecha,
            Observaciones = dto.Observaciones,
            ImagenPath = existente?.ImagenPath
        };

        if (tieneGraduaciones)
        {
            List<GraduacionDto>? graduacionesDto;
            try
            {
                graduacionesDto = JsonSerializer.Deserialize<List<GraduacionDto>>(dto.GraduacionesJson!, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                throw new ArgumentException("El formato de las graduaciones no es válido.");
            }

            if (graduacionesDto == null || graduacionesDto.Count == 0)
            {
                throw new ArgumentException("Debe incluir al menos una graduación.");
            }

            foreach (var g in graduacionesDto)
            {
                if (g.Ojo != "OD" && g.Ojo != "OI")
                {
                    throw new ArgumentException("El campo Ojo debe ser 'OD' u 'OI'.");
                }

                receta.Graduaciones.Add(new Graduacion
                {
                    Ojo = g.Ojo,
                    Esfera = g.Esfera,
                    Cilindro = g.Cilindro,
                    Eje = g.Eje,
                    Adicion = g.Adicion
                });
            }
        }

        if (tieneImagen)
        {
            var extension = Path.GetExtension(dto.Imagen!.FileName).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
            {
                throw new ArgumentException("Formato de imagen no permitido. Use jpg, jpeg, png o webp.");
            }

            if (dto.Imagen.Length > TamanoMaximoImagen)
            {
                throw new ArgumentException("La imagen no debe superar los 5MB.");
            }

            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var carpetaDestino = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "recetas");

            if (!Directory.Exists(carpetaDestino))
            {
                Directory.CreateDirectory(carpetaDestino);
            }

            var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await dto.Imagen.CopyToAsync(stream);
            }

            receta.ImagenPath = $"/uploads/recetas/{nombreArchivo}";
        }

        Receta recetaGuardada;
        try
        {
            if (existente is null)
            {
                recetaGuardada = await _recetaRepository.Crear(receta);
            }
            else
            {
                existente.ClienteId = receta.ClienteId;
                existente.Fecha = receta.Fecha;
                existente.Observaciones = receta.Observaciones;
                existente.ImagenPath = receta.ImagenPath;
                if (tieneGraduaciones)
                {
                    existente.Graduaciones.Clear();
                    existente.Graduaciones.AddRange(receta.Graduaciones);
                }
                recetaGuardada = await _recetaRepository.Actualizar(existente);
            }
        }
        catch
        {
            if (tieneImagen && receta.ImagenPath is not null)
                File.Delete(Path.Combine(_webHostEnvironment.WebRootPath, receta.ImagenPath.TrimStart('/')));
            throw;
        }
        return MapearReceta(recetaGuardada, cliente.Rut);
    }

    private static RecetaResponseDto MapearReceta(Receta recetaCreada, string rut)
    {
        return new RecetaResponseDto
        {
            Rut = rut,
            Id = recetaCreada.Id,
            ClienteId = recetaCreada.ClienteId,
            Fecha = recetaCreada.Fecha,
            Observaciones = recetaCreada.Observaciones,
            ImagenUrl = recetaCreada.ImagenPath,
            Graduaciones = recetaCreada.Graduaciones.Select(g => new GraduacionDto
            {
                Ojo = g.Ojo,
                Esfera = g.Esfera,
                Cilindro = g.Cilindro,
                Eje = g.Eje,
                Adicion = g.Adicion
            }).ToList()
        };
    }
    public async Task<List<ClienteSugerenciaDto>> BuscarSugerenciasRut(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            return new List<ClienteSugerenciaDto>();
        }

        var clientes = await _recetaRepository.BuscarClientesPorRutParcial(termino);

        return clientes.Select(c => new ClienteSugerenciaDto
        {
            Rut = c.Rut,
            Nombre = c.Nombre,
            Apellido = c.Apellido
        }).ToList();
    }
    public async Task<List<RecetaHistorialDto>> ObtenerHistorialPorRut(string rut)
    {
        var cliente = await _recetaRepository.BuscarClientePorRut(rut);

        if (cliente is null)
        {   
            throw new ArgumentException("No existe un cliente con ese RUT.");
        }

        var recetas = await _recetaRepository.ObtenerRecetasPorClienteId(cliente.IdCliente);

        return recetas.Select(r => new RecetaHistorialDto
        {
            ImagenUrl = r.ImagenPath,
            Id = r.Id,
            Fecha = r.Fecha,
            Observaciones = r.Observaciones,
            Graduaciones = r.Graduaciones.Select(g => new GraduacionHistorialDto
            {
                Ojo = g.Ojo,
                Esfera = g.Esfera,
                Cilindro = g.Cilindro,
                Eje = g.Eje,
                Adicion = g.Adicion
            }).ToList()
        }).ToList();
    }
}