using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task Actualizar_ConservaIdYReemplazaDatosSinDuplicar()
    {
        var repo = new RepositorioRecetas();
        var servicio = Servicio(repo);
        await servicio.CrearReceta(new CrearRecetaDto { Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OD\",\"esfera\":-1}]" });
        var resultado = await servicio.ActualizarReceta(11, new CrearRecetaDto
        {
            Rut = "123456785", Fecha = new DateTime(2026, 10, 6), Observaciones = "Corregida",
            GraduacionesJson = "[{\"ojo\":\"OI\",\"esfera\":-2}]"
        });
        Assert.Equal(11, resultado.Id);
        Assert.Equal("Corregida", resultado.Observaciones);
        Assert.Equal(new DateTime(2026, 10, 6), resultado.Fecha);
        Assert.Single(resultado.Graduaciones);
        Assert.Equal("OI", resultado.Graduaciones[0].Ojo);
        Assert.Equal(-2m, resultado.Graduaciones[0].Esfera);
        var cargada = await servicio.ObtenerReceta(11);
        Assert.Equal("123456785", cargada.Rut);
        Assert.Equal("Corregida", cargada.Observaciones);
    }

    [Fact]
    public async Task Actualizar_InvalidaNoModificaRecetaExistente()
    {
        var repo = new RepositorioRecetas();
        var servicio = Servicio(repo);
        await servicio.CrearReceta(new CrearRecetaDto { Rut = "123456785", Observaciones = "Original", GraduacionesJson = "[{\"ojo\":\"OD\"}]" });
        await Assert.ThrowsAsync<ArgumentException>(() => servicio.ActualizarReceta(11,
            new CrearRecetaDto { Rut = "123456785", Observaciones = "Cambio", GraduacionesJson = "no-json" }));
        Assert.Equal("Original", repo.Guardada!.Observaciones);
        Assert.Single(repo.Guardada.Graduaciones);
    }

    [Fact]
    public async Task Actualizar_ConservaImagenSinSubirlaOtraVez()
    {
        var repo = new RepositorioRecetas();
        var servicio = Servicio(repo);
        using var bytes = new MemoryStream(new byte[] { 1, 2, 3 });
        var original = await servicio.CrearReceta(new CrearRecetaDto
        { Rut = "123456785", Imagen = new FormFile(bytes, 0, 3, "imagen", "receta.png") });
        var resultado = await servicio.ActualizarReceta(11, new CrearRecetaDto
        { Rut = "123456785", Observaciones = "Control" });
        Assert.Equal(original.ImagenUrl, resultado.ImagenUrl);
        Assert.Equal("Control", resultado.Observaciones);
        Assert.Single(Directory.GetFiles(Path.Combine(carpeta, "uploads", "recetas")));
        Assert.Equal(original.ImagenUrl, (await servicio.ObtenerHistorialPorRut("123456785"))[0].ImagenUrl);
    }

    [Fact]
    public async Task Actualizar_RecetaInexistenteNoCreaOtra()
    {
        var repo = new RepositorioRecetas();
        var servicio = Servicio(repo);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => servicio.ActualizarReceta(99, new CrearRecetaDto()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => servicio.ObtenerReceta(99));
        Assert.Null(repo.Guardada);
    }

    [Fact]
    public async Task Actualizar_PersisteGraduacionesSinConservarFilasAnteriores()
    {
        var opciones = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Optica.Api.Data.OpticaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var contexto = new Optica.Api.Data.OpticaDbContext(opciones);
        contexto.Clientes.Add(new Cliente { IdCliente = 7, Rut = "123456785" });
        await contexto.SaveChangesAsync();
        var servicio = new RecetaService(new RecetaRepository(contexto), new EntornoPrueba { WebRootPath = carpeta });
        var creada = await servicio.CrearReceta(new CrearRecetaDto { Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OD\",\"esfera\":-1}]" });
        await servicio.ActualizarReceta(creada.Id, new CrearRecetaDto { Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OI\",\"esfera\":-3}]" });
        contexto.ChangeTracker.Clear();
        var guardada = await servicio.ObtenerReceta(creada.Id);
        Assert.Single(await contexto.Recetas.ToListAsync());
        Assert.Single(await contexto.Graduaciones.ToListAsync());
        Assert.Equal("OI", guardada.Graduaciones[0].Ojo);
        Assert.Equal(-3m, guardada.Graduaciones[0].Esfera);
    }

    [Theory]
    [InlineData("12345", "Ana")]
    [InlineData("ana", "Ana")]
    [InlineData("Pérez", "Ana")]
    [InlineData("Ana Pérez", "Ana")]
    public async Task Sugerencias_BuscaPorRutNombreYApellido(string termino, string esperado)
    {
        var repositorio = new RepositorioRecetas
        {
            Cliente = new Cliente { IdCliente = 1, Rut = "12.345.678-5", Nombre = "Ana", Apellido = "Pérez" }
        };
        var servicio = Servicio(repositorio);
        var resultados = await servicio.BuscarSugerenciasRut(termino);
        Assert.Single(resultados);
        Assert.Equal(esperado, resultados[0].Nombre);
    }

    [Fact]
    public async Task CrearYActualizar_ConservaImagenYGraduacionesJuntas()
    {
        var repo = new RepositorioRecetas();
        var servicio = Servicio(repo);
        using var bytes = new MemoryStream(new byte[] { 1, 2, 3 });
        var creada = await servicio.CrearReceta(new CrearRecetaDto
        {
            Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OD\",\"esfera\":-1}]",
            Imagen = new FormFile(bytes, 0, 3, "imagen", "receta.png")
        });
        Assert.Single(creada.Graduaciones);
        Assert.NotNull(creada.ImagenUrl);
        var actualizada = await servicio.ActualizarReceta(11, new CrearRecetaDto
        { Rut = "123456785", GraduacionesJson = "[{\"ojo\":\"OD\",\"esfera\":-2}]" });
        Assert.Equal(creada.ImagenUrl, actualizada.ImagenUrl);
        Assert.Equal(-2m, actualizada.Graduaciones[0].Esfera);
        using var nuevosBytes = new MemoryStream(new byte[] { 4, 5, 6 });
        var conNuevaImagen = await servicio.ActualizarReceta(11, new CrearRecetaDto
        { Rut = "123456785", Imagen = new FormFile(nuevosBytes, 0, 3, "imagen", "nueva.png") });
        Assert.NotEqual(creada.ImagenUrl, conNuevaImagen.ImagenUrl);
        Assert.Single(conNuevaImagen.Graduaciones);
        Assert.Equal(-2m, conNuevaImagen.Graduaciones[0].Esfera);
        var historial = await servicio.ObtenerHistorialPorRut("123456785");
        Assert.NotNull(historial[0].ImagenUrl);
        Assert.Single(historial[0].Graduaciones);
    }

    private sealed class RepositorioRecetas : IRecetaRepository
    {
        public Cliente? Cliente { get; set; } = new() { IdCliente = 7, Rut = "123456785" };
        public Receta? Guardada { get; private set; }
        public Task<Cliente?> BuscarClientePorRut(string rut) => Task.FromResult(Cliente);
        public Task<Receta> Crear(Receta receta) { receta.Id = 11; Guardada = receta; return Task.FromResult(receta); }
        public Task<Receta> Actualizar(Receta receta) { Guardada = receta; return Task.FromResult(receta); }
        public Task<Cliente?> ObtenerClientePorId(int id) => Task.FromResult(Cliente);
        public Task<Receta?> ObtenerPorId(int id) => Task.FromResult(Guardada);
        public Task<List<Cliente>> BuscarClientesPorRutParcial(string rutParcial)
        {
            var clientes = new List<Cliente>();
            if (Cliente is not null && (RutChilenoValidator.Normalizar(Cliente.Rut).StartsWith(
                RutChilenoValidator.Normalizar(rutParcial), StringComparison.OrdinalIgnoreCase)
                || $"{Cliente.Nombre} {Cliente.Apellido}".Contains(rutParcial.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                clientes.Add(Cliente);
            }
            return Task.FromResult(clientes);
        }

        public Task<List<Receta>> ObtenerRecetasPorClienteId(int clienteId)
        {
            var recetas = new List<Receta>();
            if (Guardada is not null && Guardada.ClienteId == clienteId)
            {
                recetas.Add(Guardada);
            }
            return Task.FromResult(recetas);
        }
    }
}
