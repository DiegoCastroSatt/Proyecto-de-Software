using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Optica.Api.Modules.Accesos;
using Optica.Api.Modules.Autenticacion.Controllers;
using Xunit;

namespace Optica.Tests;

public class TestPoliticasRecetas
{
    [Theory]
    [InlineData(nameof(RecetasController.ObtenerMisRecetas), "cliente", false, true)]
    [InlineData(nameof(RecetasController.ObtenerMisRecetas), "Trabajador", true, false)]
    [InlineData(nameof(RecetasController.ObtenerReceta), "cliente", false, false)]
    [InlineData(nameof(RecetasController.CrearReceta), "Trabajador", true, true)]
    [InlineData(nameof(RecetasController.CrearReceta), "Trabajador", false, false)]
    [InlineData(nameof(RecetasController.ActualizarReceta), "Administrador", false, true)]
    public async Task CombinaLasPoliticasSinBloquearAlClienteNiAbrirElAccesoDelPersonal(
        string accion, string tipo, bool permisoRecetas, bool permitido)
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddAuthorization(options =>
        {
            PermisosAcceso.Configurar(options);
            options.AddPolicy(AutenticacionClientesController.Politica, p => p
                .AddAuthenticationSchemes(AutenticacionClientesController.Esquema)
                .RequireAuthenticatedUser().RequireClaim(AutenticacionClientesController.ClaimTipo, "cliente"));
        });
        using var provider = servicios.BuildServiceProvider();
        var datos = typeof(RecetasController).GetCustomAttributes<AuthorizeAttribute>()
            .Concat(typeof(RecetasController).GetMethod(accion)!.GetCustomAttributes<AuthorizeAttribute>());
        var politica = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(), datos);
        Assert.NotNull(politica);
        var claims = new List<Claim> { new(PermisosAcceso.Tipo, tipo) };
        if (permisoRecetas) claims.Add(new Claim(PermisosAcceso.Permiso, "recetas"));
        var usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "Prueba"));
        var resultado = await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(usuario, null, politica!);
        Assert.Equal(permitido, resultado.Succeeded);
    }
}
