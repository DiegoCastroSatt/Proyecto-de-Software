using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Optica.Api.Data;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.DTOs;
using Xunit;

namespace Optica.Tests;

public sealed class TestAccesos
{
    private static OpticaDbContext CrearDb() => new(new DbContextOptionsBuilder<OpticaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ClaimsPrincipal Principal(string tipo, params string[] permisos) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "Prueba"), new Claim(PermisosAcceso.Tipo, tipo)
    }.Concat(permisos.Select(p => new Claim(PermisosAcceso.Permiso, p))), "Pruebas"));

    [Fact]
    public void AdministradorTieneTodosLosPermisosYClienteNinguno()
    {
        foreach (var permiso in PermisosAcceso.Catalogo.Keys)
        {
            Assert.True(PermisosAcceso.Tiene(Principal("Administrador"), permiso));
            Assert.False(PermisosAcceso.Tiene(Principal("Cliente", permiso), permiso));
            Assert.False(PermisosAcceso.Tiene(new ClaimsPrincipal(new ClaimsIdentity()), permiso));
        }
    }

    [Fact]
    public async Task SesionReleeRolesYRevocaTrabajadoresInactivosSinConfundirIds()
    {
        await using var db = CrearDb();
        db.Administradores.Add(new Administrador { IdAdministrador = 1, Nombre = "Admin", NombreUsuario = "admin", Estado = "Activo" });
        var rol = new RolTrabajador { IdRol = 1, Nombre = "Ventas", PermisosJson = "[\"ventas\"]" };
        db.Trabajadores.Add(new Trabajador { IdTrabajador = 2, Nombre = "Personal", NombreUsuario = "personal", Correo = "EQUIPO@EXAMPLE.COM", IdRol = 1, Rol = rol, IdAdministradorCreador = 1 });
        await db.SaveChangesAsync();
        var servicio = new AccesoService(db, new PasswordHasher<Trabajador>());
        var principal = AccesoService.CrearPrincipal(AccesoService.Respuesta(await db.Trabajadores.Include(t => t.Rol).SingleAsync(t => !(t is Administrador))));
        Assert.Contains("ventas", (await servicio.ObtenerSesionAsync(principal, default))!.Permisos);
        rol.PermisosJson = "[\"agenda\"]";
        await db.SaveChangesAsync();
        var sesion = (await servicio.ObtenerSesionAsync(principal, default))!;
        Assert.DoesNotContain("ventas", sesion.Permisos);
        Assert.Contains("agenda", sesion.Permisos);
        Assert.Equal(0, sesion.IdAdministrador);
        Assert.Equal("Trabajador", sesion.TipoUsuario);
        var trabajador = await db.Trabajadores.FindAsync(2);
        trabajador!.Estado = "Inactivo";
        await db.SaveChangesAsync();
        Assert.Null(await servicio.ObtenerSesionAsync(principal, default));
        var admin = await db.Administradores.SingleAsync();
        Assert.NotNull(await servicio.ObtenerSesionAsync(AccesoService.CrearPrincipal(new LoginAdministradorResponse(1, admin.Nombre) { VersionCredencial = AccesoService.CrearVersionCredencial(admin.Contrasena) }), default));
        Assert.Null(await servicio.ObtenerSesionAsync(Principal("Cliente"), default));
    }

    [Fact]
    public async Task LoginTrabajadorValidaHashYCorreoSinCrearPermisosDeAdministrador()
    {
        await using var db = CrearDb();
        var hasher = new PasswordHasher<Trabajador>();
        var trabajador = new Trabajador { IdTrabajador = 1, Nombre = "Equipo", NombreUsuario = "equipo", Correo = "EQUIPO@EXAMPLE.COM", Rol = new RolTrabajador { Nombre = "Agenda", PermisosJson = "[\"agenda\",\"SoloAdministrador\"]" } };
        var clave = (Guid.NewGuid().ToString("N") + "A1!");
        trabajador.Contrasena = hasher.HashPassword(trabajador, clave);
        db.Trabajadores.Add(trabajador); await db.SaveChangesAsync();
        var servicio = new AccesoService(db, hasher);
        Assert.Null(await servicio.IniciarTrabajadorAsync(new() { NombreUsuario = "equipo", Contrasena = "incorrecta" }, default));
        var sesion = await servicio.IniciarTrabajadorAsync(new() { NombreUsuario = "equipo", Contrasena = clave }, default);
        Assert.NotNull(sesion);
        var principal = AccesoService.CrearPrincipal(sesion);
        Assert.True(PermisosAcceso.Tiene(principal, "agenda"));
        Assert.False(PermisosAcceso.Tiene(principal, "ventas"));
        Assert.False(principal.HasClaim(PermisosAcceso.Tipo, "Administrador"));
        Assert.DoesNotContain("SoloAdministrador", sesion.Permisos);
    }

    private static async Task<IHost> CrearServidor()
    {
        var nombreDb = Guid.NewGuid().ToString();
        return await new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(s =>
        {
            s.AddDbContext<OpticaDbContext>(o => o.UseInMemoryDatabase(nombreDb));
            s.AddScoped<IPasswordHasher<Trabajador>, PasswordHasher<Trabajador>>();
            s.AddLogging();
            s.AddScoped<ICatalogoRepository, CatalogoRepository>();
            s.AddScoped<ICatalogoService, CatalogoService>();
            s.AddScoped<IProductoRepository, ProductoRepository>();
            s.AddScoped<IProductoService, ProductoService>();
            s.AddControllers().AddApplicationPart(typeof(AccesosController).Assembly);
            s.AddDataProtection().UseEphemeralDataProtectionProvider();
            s.AddAuthentication("Pruebas").AddCookie().AddScheme<AuthenticationSchemeOptions, AutenticacionPrueba>("Pruebas", _ => { });
            s.AddAuthorization(PermisosAcceso.Configurar);
        }).Configure(app =>
        {
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(e => e.MapControllers());
        })).StartAsync();
    }

    [Theory]
    [InlineData("", 401)]
    [InlineData("Trabajador", 403)]
    [InlineData("Cliente", 403)]
    public async Task ApiBloqueaGestionDeTrabajadoresYRoles(string tipo, int esperado)
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        if (tipo.Length > 0) cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", tipo);
        Assert.Equal(esperado, (int)(await cliente.GetAsync("/api/Accesos/trabajadores")).StatusCode);
        Assert.Equal(esperado, (int)(await cliente.PostAsJsonAsync("/api/Accesos/roles", new { nombre = "Intento", permisos = new[] { "ventas" } })).StatusCode);
        Assert.Equal(esperado, (int)(await cliente.PostAsJsonAsync("/api/Accesos/trabajadores", new { nombre = "Intento" })).StatusCode);
        Assert.Equal(esperado, (int)(await cliente.PutAsJsonAsync("/api/Accesos/trabajadores/1/acceso", new { idRol = 1, estado = "Activo" })).StatusCode);
    }

    [Theory]
    [InlineData("/api/Clientes/buscar")]
    [InlineData("/api/Ventas")]
    [InlineData("/api/Reservas/agenda")]
    [InlineData("/api/Pedidos")]
    [InlineData("/api/Horarios")]
    [InlineData("/api/Recetas/historial")]
    [InlineData("/api/Dashboard/summary")]
    [InlineData("/api/Catalogos")]
    public async Task ApiBloqueaModulosSinPermiso(string ruta)
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", "Trabajador");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
    }

    [Fact]
    public async Task TrabajadorSoloPuedeOperarSuModuloYCatalogoSiguePublico()
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/Productos")).StatusCode);
        cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", "Trabajador");
        cliente.DefaultRequestHeaders.Add("X-Prueba-Permisos", "inventario");
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync("/api/Catalogos", new { tipo = "Marca", nombre = "Marca de prueba" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/Ventas", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/Accesos/trabajadores")).StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/Clientes")]
    [InlineData("POST", "/api/Ventas")]
    [InlineData("POST", "/api/Pedidos")]
    [InlineData("POST", "/api/Horarios")]
    [InlineData("POST", "/api/Recetas")]
    [InlineData("POST", "/api/Reservas/1/cancelar")]
    [InlineData("DELETE", "/api/Productos/1")]
    public async Task ApiRechazaCambiosDirectosSinPermiso(string metodo, string ruta)
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", "Trabajador");
        using var peticion = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(peticion)).StatusCode);
    }

    [Theory]
    [InlineData("", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Administrador", 403)]
    public async Task CambioDeClaveSoloDisponibleParaTrabajadorAutenticado(string tipo, int esperado)
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        if (tipo.Length > 0) cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", tipo);
        Assert.Equal(esperado, (int)(await cliente.PostAsJsonAsync("/api/CuentaTrabajador/contrasena", new { contrasenaActual = "anterior", nuevaContrasena = "nueva-de-prueba-123", confirmarContrasena = "nueva-de-prueba-123" })).StatusCode);
    }

    [Fact]
    public async Task CambioClaveValidaActualConfirmacionYNoPermiteCambiarOtraCuenta()
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", "Trabajador");
        var anterior = (Guid.NewGuid().ToString("N") + "A1!"); var nueva = (Guid.NewGuid().ToString("N") + "A1!");
        var hasher = new PasswordHasher<Trabajador>();
        ClaimsPrincipal cookieAnterior;
        string hashAnterior;
        using (var scope = servidor.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
            var rol = new RolTrabajador { IdRol = 1, Nombre = "Sin permisos", PermisosJson = "[]" };
            var cuenta = new Trabajador { IdTrabajador = 1, Nombre = "Cuenta propia", NombreUsuario = "propia", Correo = "PROPIA@EXAMPLE.COM", IdRol = 1, Rol = rol };
            cuenta.Contrasena = hasher.HashPassword(cuenta, anterior); hashAnterior = cuenta.Contrasena;
            db.Trabajadores.Add(cuenta);
            db.Trabajadores.Add(new Trabajador { IdTrabajador = 2, Nombre = "Otra cuenta", NombreUsuario = "otra", Correo = "OTRA@EXAMPLE.COM", IdRol = 1, Rol = rol, Contrasena = hashAnterior });
            await db.SaveChangesAsync(); cookieAnterior = AccesoService.CrearPrincipal(AccesoService.Respuesta(cuenta));
        }
        var ruta = "/api/CuentaTrabajador/contrasena";
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { contrasenaActual = "incorrecta", nuevaContrasena = nueva, confirmarContrasena = nueva })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { contrasenaActual = anterior, nuevaContrasena = nueva, confirmarContrasena = "no-coincide" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { contrasenaActual = anterior, nuevaContrasena = "corta", confirmarContrasena = "corta" })).StatusCode);
        using (var scope = servidor.Services.CreateScope())
            Assert.Equal(hashAnterior, (await scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Trabajadores.FindAsync(1))!.Contrasena);
        var resultado = await cliente.PostAsJsonAsync(ruta, new { contrasenaActual = anterior, nuevaContrasena = nueva, confirmarContrasena = nueva, idTrabajador = 2 });
        Assert.Equal(HttpStatusCode.OK, resultado.StatusCode);
        Assert.Contains(resultado.Headers, h => h.Key == "Set-Cookie");
        using var scopeFinal = servidor.Services.CreateScope();
        var contexto = scopeFinal.ServiceProvider.GetRequiredService<OpticaDbContext>();
        var propia = (await contexto.Trabajadores.FindAsync(1))!;
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(propia, propia.Contrasena, nueva));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(propia, propia.Contrasena, anterior));
        Assert.Equal(hashAnterior, (await contexto.Trabajadores.FindAsync(2))!.Contrasena);
        var servicio = new AccesoService(contexto, hasher);
        Assert.Null(await servicio.ObtenerSesionAsync(cookieAnterior, default));
        Assert.NotNull(await servicio.IniciarTrabajadorAsync(new() { NombreUsuario = "propia", Contrasena = nueva }, default));
    }

    [Fact]
    public async Task AdministradorCreaRolYTrabajadorConHashSinExponerContrasena()
    {
        using var servidor = await CrearServidor(); using var cliente = servidor.GetTestClient();
        cliente.DefaultRequestHeaders.Add("X-Prueba-Tipo", "Administrador");
        var rol = await cliente.PostAsJsonAsync("/api/Accesos/roles", new { nombre = "Ventas", permisos = new[] { "ventas" } });
        Assert.Equal(HttpStatusCode.OK, rol.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await rol.Content.ReadAsStringAsync());
        var idRol = json.RootElement.GetProperty("idRol").GetInt32();
        var clave = (Guid.NewGuid().ToString("N") + "A1!");
        var resultado = await cliente.PostAsJsonAsync("/api/Accesos/trabajadores", new { nombre = "Equipo", nombreUsuario = "equipo", correo = "equipo@example.com", contrasena = clave, idRol });
        Assert.Equal(HttpStatusCode.OK, resultado.StatusCode);
        using var scope = servidor.Services.CreateScope();
        var trabajador = await scope.ServiceProvider.GetRequiredService<OpticaDbContext>().Trabajadores.SingleAsync();
        Assert.NotEqual(clave, trabajador.Contrasena);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<Trabajador>().VerifyHashedPassword(trabajador, trabajador.Contrasena, clave));
        var listado = await cliente.GetStringAsync("/api/Accesos/trabajadores");
        Assert.DoesNotContain(clave, listado);
        Assert.DoesNotContain("contrasena", listado);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/Accesos/trabajadores", new { nombre = "Otro", nombreUsuario = "EQUIPO", correo = "EQUIPO@EXAMPLE.COM", contrasena = clave, idRol })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/Accesos/roles", new { nombre = "Elevacion", permisos = new[] { "SoloAdministrador" } })).StatusCode);
    }
}

public sealed class AutenticacionPrueba(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tipo = Request.Headers["X-Prueba-Tipo"].ToString();
        if (tipo.Length == 0) return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1"), new(PermisosAcceso.Tipo, tipo) };
        claims.AddRange(Request.Headers["X-Prueba-Permisos"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => new Claim(PermisosAcceso.Permiso, p)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
