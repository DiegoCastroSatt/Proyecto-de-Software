using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestRecetasController
{
    [Fact]
    public async Task CrearReceta_Devuelve200ConLaRecetaCreada()
    {
        var esperado = new RecetaResponseDto { Id = 5, Rut = "12345678-9" };
        var servicio = new ServicioRecetasFalso { Receta = esperado };

        var resultado = await new RecetasController(servicio).CrearReceta(
            new CrearRecetaDto { Rut = "12345678-9", Fecha = DateTime.Today });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task CrearReceta_Devuelve400CuandoLosDatosSonInvalidos()
    {
        var servicio = new ServicioRecetasFalso
        {
            ErrorAlCrear = new ArgumentException("Cliente no encontrado.")
        };

        var resultado = await new RecetasController(servicio).CrearReceta(
            new CrearRecetaDto { Rut = "0-0", Fecha = DateTime.Today });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task ObtenerReceta_Devuelve200ConLaRecetaEncontrada()
    {
        var esperado = new RecetaResponseDto { Id = 8 };
        var servicio = new ServicioRecetasFalso { Receta = esperado };

        var resultado = await new RecetasController(servicio).ObtenerReceta(8);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task ObtenerReceta_Devuelve404CuandoNoExiste()
    {
        var servicio = new ServicioRecetasFalso
        {
            ErrorAlObtener = new KeyNotFoundException("Receta no encontrada.")
        };

        var resultado = await new RecetasController(servicio).ObtenerReceta(999);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task ActualizarReceta_Devuelve200ConLaRecetaActualizada()
    {
        var esperado = new RecetaResponseDto { Id = 3, Observaciones = "Actualizada" };
        var servicio = new ServicioRecetasFalso { Receta = esperado };

        var resultado = await new RecetasController(servicio).ActualizarReceta(
            3, new CrearRecetaDto { Rut = "12345678-9", Fecha = DateTime.Today });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task ActualizarReceta_Devuelve404CuandoLaRecetaNoExiste()
    {
        var servicio = new ServicioRecetasFalso
        {
            ErrorAlActualizar = new KeyNotFoundException("Receta no encontrada.")
        };

        var resultado = await new RecetasController(servicio).ActualizarReceta(
            404, new CrearRecetaDto { Rut = "12345678-9", Fecha = DateTime.Today });

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task ActualizarReceta_Devuelve400CuandoLosDatosSonInvalidos()
    {
        var servicio = new ServicioRecetasFalso
        {
            ErrorAlActualizar = new ArgumentException("Graduaciones inválidas.")
        };

        var resultado = await new RecetasController(servicio).ActualizarReceta(
            3, new CrearRecetaDto { Rut = "12345678-9", Fecha = DateTime.Today });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task BuscarSugerenciasRut_Devuelve200AunqueNoHayaCoincidencias()
    {
        var resultado = await new RecetasController(new ServicioRecetasFalso())
            .BuscarSugerenciasRut("999");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<ClienteSugerenciaDto>>(ok.Value));
    }

    [Fact]
    public async Task BuscarSugerenciasRut_Devuelve200ConLasCoincidenciasEncontradas()
    {
        var esperado = new List<ClienteSugerenciaDto>
        {
            new() { Rut = "12345671-8", Nombre = "María", Apellido = "González" }
        };
        var servicio = new ServicioRecetasFalso { Sugerencias = esperado };

        var resultado = await new RecetasController(servicio).BuscarSugerenciasRut("Gonz");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task ObtenerHistorialPorRut_Devuelve200ConElHistorial()
    {
        var esperado = new List<RecetaHistorialDto> { new() { Id = 1 } };
        var servicio = new ServicioRecetasFalso { Historial = esperado };

        var resultado = await new RecetasController(servicio).ObtenerHistorialPorRut("12345678-9");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(esperado, ok.Value);
    }

    [Fact]
    public async Task ObtenerHistorialPorRut_Devuelve400CuandoElRutEsInvalido()
    {
        var servicio = new ServicioRecetasFalso
        {
            ErrorAlObtenerHistorial = new ArgumentException("Rut inválido.")
        };

        var resultado = await new RecetasController(servicio).ObtenerHistorialPorRut("abc");

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    private sealed class ServicioRecetasFalso : IRecetaService
    {
        public RecetaResponseDto? Receta { get; set; }
        public List<ClienteSugerenciaDto> Sugerencias { get; set; } = [];
        public List<RecetaHistorialDto> Historial { get; set; } = [];
        public Exception? ErrorAlCrear { get; set; }
        public Exception? ErrorAlObtener { get; set; }
        public Exception? ErrorAlActualizar { get; set; }
        public Exception? ErrorAlObtenerHistorial { get; set; }

        public Task<RecetaResponseDto> CrearReceta(CrearRecetaDto dto)
        {
            if (ErrorAlCrear is not null)
                return Task.FromException<RecetaResponseDto>(ErrorAlCrear);
            return Task.FromResult(Receta ?? new RecetaResponseDto { Rut = dto.Rut });
        }

        public Task<RecetaResponseDto> ObtenerReceta(int id)
        {
            if (ErrorAlObtener is not null)
                return Task.FromException<RecetaResponseDto>(ErrorAlObtener);
            return Task.FromResult(Receta ?? new RecetaResponseDto { Id = id });
        }

        public Task<RecetaResponseDto> ActualizarReceta(int id, CrearRecetaDto dto)
        {
            if (ErrorAlActualizar is not null)
                return Task.FromException<RecetaResponseDto>(ErrorAlActualizar);
            return Task.FromResult(Receta ?? new RecetaResponseDto { Id = id, Rut = dto.Rut });
        }

        public Task<List<ClienteSugerenciaDto>> BuscarSugerenciasRut(string termino)
            => Task.FromResult(Sugerencias);

        public Task<List<RecetaHistorialDto>> ObtenerHistorialPorRut(string rut)
        {
            if (ErrorAlObtenerHistorial is not null)
                return Task.FromException<List<RecetaHistorialDto>>(ErrorAlObtenerHistorial);
            return Task.FromResult(Historial);
        }
    }
}