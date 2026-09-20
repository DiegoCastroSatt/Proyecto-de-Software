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

    public async Task<RecetaResponseDto> CrearReceta(CrearRecetaDto dto)
    {
        var cliente = await _recetaRepository.BuscarClientePorRut(dto.Rut);
        if (cliente == null)
        {
            throw new ArgumentException("No existe un cliente con ese RUT.");
        }

        var tieneGraduaciones = !string.IsNullOrWhiteSpace(dto.GraduacionesJson);
        var tieneImagen = dto.Imagen != null;

        if (!tieneGraduaciones && !tieneImagen)
        {
            throw new ArgumentException("Debe registrar la receta escrita o mediante una imagen.");
        }

        var receta = new Receta
        {
            ClienteId = cliente.IdCliente,
            Fecha = dto.Fecha,
            Observaciones = dto.Observaciones
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

        var recetaCreada = await _recetaRepository.Crear(receta);

        return new RecetaResponseDto
        {
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
}