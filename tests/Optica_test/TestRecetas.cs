using Microsoft.AspNetCore.Http;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.RegistroRecetas.Models;
using Xunit;

namespace Optica.Ventas.Tests;

public class TestRecetas : IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "optica-recetas-" + Guid.NewGuid());
    private RecetaService Servicio(RepositorioRecetas repo) => new(repo, new EntornoPrueba { WebRootPath = carpeta });
    public void Dispose() { if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true); }

    [Fact]
    public async Task Crear_ConservaGraduacionesDeAmbosOjos()
    {
        var repo = new RepositorioRecetas();
        var fecha = new DateTime(2026, 10, 1);
        var resultado = await Servicio(repo).CrearReceta(new CrearRecetaDto
        {
            Rut = "123456785", Fecha = fecha, Observaciones = "Control",
            GraduacionesJson = """[{"ojo":"OD","esfera":-1.25,"cilindro":-0.5,"eje":90,"adicion":1},{"ojo":"OI","esfera":-2}]"""
        });
        Assert.Equal(11, resultado.Id);
        Assert.Equal(7, resultado.ClienteId);
        Assert.Equal(fecha, resultado.Fecha);
        Assert.Equal("Control", resultado.Observaciones);
        Assert.Equal(2, resultado.Graduaciones.Count);
        Assert.Equal(-1.25m, resultado.Graduaciones[0].Esfera);
        Assert.Equal(-0.5m, resultado.Graduaciones[0].Cilindro);
        Assert.Equal(90, resultado.Graduaciones[0].Eje);
        Assert.Equal(1m, resultado.Graduaciones[0].Adicion);
        Assert.Equal("OI", repo.Guardada!.Graduaciones[1].Ojo);
        Assert.False(Directory.Exists(carpeta));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[{\"ojo\":\"XX\"}]")]
    public async Task Crear_RechazaRecetaEscritaInvalidaSinGuardar(string? json)
    {
        var repo = new RepositorioRecetas();
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio(repo).CrearReceta(new CrearRecetaDto { Rut = "123456785", GraduacionesJson = json }));
        Assert.Null(repo.Guardada);
        Assert.False(Directory.Exists(carpeta));
    }

    [Fact]
    public async Task Crear_RechazaClienteInexistente()
    {
        var repo = new RepositorioRecetas { Cliente = null };
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio(repo).CrearReceta(new CrearRecetaDto { Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OD\"}]" }));
        Assert.Null(repo.Guardada);
    }

    [Theory]
    [InlineData("receta.pdf", 10)]
    [InlineData("receta.png", 5242881)]
    public async Task Crear_RechazaImagenNoPermitidaSinEscribir(string nombre, int longitud)
    {
        var repo = new RepositorioRecetas();
        using var contenido = new MemoryStream(new byte[longitud]);
        var imagen = new FormFile(contenido, 0, longitud, "imagen", nombre);
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio(repo).CrearReceta(new CrearRecetaDto { Rut = "123456785", Imagen = imagen }));
        Assert.Null(repo.Guardada);
        Assert.False(Directory.Exists(carpeta));
    }

    [Fact]
    public async Task Crear_ImagenGuardaArchivoYReferencia()
    {
        var repo = new RepositorioRecetas();
        byte[] bytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
        using var contenido = new MemoryStream(bytes);
        var resultado = await Servicio(repo).CrearReceta(new CrearRecetaDto
        { Rut = "123456785", Imagen = new FormFile(contenido, 0, bytes.Length, "imagen", "receta.PNG") });
        Assert.StartsWith("/uploads/recetas/", resultado.ImagenUrl);
        Assert.EndsWith(".png", resultado.ImagenUrl);
        Assert.Equal(resultado.ImagenUrl, repo.Guardada!.ImagenPath);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(carpeta, resultado.ImagenUrl!.TrimStart('/'))));
    }

    private sealed class RepositorioRecetas : IRecetaRepository
    {
        public Cliente? Cliente { get; set; } = new() { IdCliente = 7, Rut = "123456785" };
        public Receta? Guardada { get; private set; }
        public Task<Cliente?> BuscarClientePorRut(string rut) => Task.FromResult(Cliente);
        public Task<Receta> Crear(Receta receta) { receta.Id = 11; Guardada = receta; return Task.FromResult(receta); }
        public Task<Receta?> ObtenerPorId(int id) => Task.FromResult(Guardada);
        public Task<List<Cliente>> BuscarClientesPorRutParcial(string rutParcial) =>
            Task.FromResult(Cliente is not null && RutChilenoValidator.Normalizar(Cliente.Rut)
                .StartsWith(RutChilenoValidator.Normalizar(rutParcial), StringComparison.OrdinalIgnoreCase)
                ? new List<Cliente> { Cliente } : new List<Cliente>());
        public Task<List<Receta>> ObtenerRecetasPorClienteId(int clienteId) =>
            Task.FromResult(Guardada is not null && Guardada.ClienteId == clienteId
                ? new List<Receta> { Guardada } : new List<Receta>());
    }
}
