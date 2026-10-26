using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Repositories;
using Optica.Api.Modules.Ventas.Services;
using Optica.Api.Modules.Pedidos.Interfaces;
using Optica.Api.Modules.Pedidos.Repositories;
using Optica.Api.Modules.Pedidos.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Optica.Api.Data;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Autenticacion.Interfaces;
using Optica.Api.Modules.Autenticacion.Repositories;
using Optica.Api.Modules.Autenticacion.Services;
using Optica.Api.Modules.Autenticacion.Controllers;
using Optica.Api.Modules.Clientes.Interfaces;
using Optica.Api.Modules.Clientes.Repositories;
using Optica.Api.Modules.Clientes.Services;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.AgendaReservas.Interfaces;
using Optica.Api.Modules.AgendaReservas.Services;
using Optica.Api.Modules.Notificaciones.Interfaces;
using Optica.Api.Modules.Notificaciones.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OpticaDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("OpticaDb")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'OpticaDb'.")
    )
);
var rutaClavesProteccion = builder.Configuration["DataProtection:KeysPath"];
var proteccionDatos = builder.Services.AddDataProtection();
if (!string.IsNullOrWhiteSpace(rutaClavesProteccion))
    proteccionDatos.PersistKeysToFileSystem(new DirectoryInfo(rutaClavesProteccion));
// Registra controllers, OpenAPI y el acceso del frontend Angular.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4300")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Implementación temporal hasta configurar la base de datos.
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IReservaCorreoService, ReservaCorreoService>();
builder.Services.AddSingleton<ColaCorreosReprogramacion>();
builder.Services.AddScoped<IReservaService>(proveedor => new ReservaService(
    proveedor.GetRequiredService<IReservaRepository>(),
    proveedor.GetRequiredService<IReservaCorreoService>(),
    proveedor.GetRequiredService<TimeProvider>(),
    proveedor.GetRequiredService<ILogger<ReservaService>>(),
    null,
    proveedor.GetRequiredService<OpticaDbContext>()));
builder.Services.AddScoped<IRecordatorioReservasService, RecordatorioReservasService>();
builder.Services.AddHostedService<RecordatorioReservasBackgroundService>();
builder.Services.AddHostedService<CorreoReservaBackgroundService>();
builder.Services.AddScoped<IRecetaRepository, RecetaRepository>();
builder.Services.AddScoped<IRecetaService, RecetaService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IConsultaProductoVenta, ConsultaProductoVenta>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<ICalculoVenta, CalculoVenta>();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoCorreoService, PedidoCorreoService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddHostedService<CorreoPedidoBackgroundService>();
builder.Services.AddScoped<IPasswordHasher<Administrador>, PasswordHasher<Administrador>>();
builder.Services.AddScoped<IPasswordHasher<CuentaCliente>, PasswordHasher<CuentaCliente>>();
builder.Services.AddScoped<AutenticacionClienteService>();
builder.Services.AddSingleton<EstadoRecuperacionAdministrador>();
builder.Services.AddSingleton<EstadoRecuperacionCliente>();
builder.Services.AddScoped<IAutenticacionAdministradorRepository, AutenticacionAdministradorRepository>();
builder.Services.AddScoped<IAutenticacionAdministradorService, AutenticacionAdministradorService>();

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "Optica.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<OpticaDbContext>();
            if (!int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                || !await db.Administradores.AnyAsync(a => a.IdAdministrador == id && a.Estado == "Activo", ctx.HttpContext.RequestAborted))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync();
            }
        };
    })
    .AddCookie(AutenticacionClientesController.Esquema, options =>
    {
        options.Cookie.Name = "Optica.Cliente";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var db = ctx.HttpContext.RequestServices.GetRequiredService<OpticaDbContext>();
            if (!int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var idCliente)
                || !await db.CuentasCliente.AnyAsync(c => c.IdCliente == idCliente
                    && c.Estado == "Activa" && c.Cliente!.Estado == "Activo", ctx.HttpContext.RequestAborted))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(AutenticacionClientesController.Esquema);
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    // La política predeterminada y de respaldo conserva la autenticación administrativa existente.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(AutenticacionClientesController.Politica, policy =>
    {
        policy.AddAuthenticationSchemes(AutenticacionClientesController.Esquema);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(AutenticacionClientesController.ClaimTipo, "cliente");
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseCors("Angular");
app.UseAuthentication();
// Las operaciones con cookie requieren el origen conocido del frontend para evitar CSRF.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api")
        && context.Request.Method is not ("GET" or "HEAD" or "OPTIONS")
        && (context.User.Identity?.IsAuthenticated == true
            || context.Request.Cookies.ContainsKey("Optica.Cliente")
            || context.Request.Path == "/api/Autenticacion/admin"
            || context.Request.Path.StartsWithSegments("/api/Autenticacion/cliente")))
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (origin != "http://localhost:4200" && origin != "http://localhost:4300"
            && origin != $"{context.Request.Scheme}://{context.Request.Host}")
        {
            context.Response.StatusCode = 403;
            return;
        }
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();

app.MapGet("/test-db", async (OpticaDbContext db) =>
{
    var cantidad = await db.Clientes.CountAsync();

    return Results.Ok(new
    {
        mensaje = "Conexión a MySQL funcionando",
        clientes = cantidad
    });
});

app.Run();
