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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OpticaDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("OpticaDb")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'OpticaDb'.")
    )
);
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
            .AllowAnyMethod();
    });
});

// Implementación temporal hasta configurar la base de datos.
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();
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
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IPasswordHasher<Administrador>, PasswordHasher<Administrador>>();
builder.Services.AddScoped<IAutenticacionAdministradorRepository, AutenticacionAdministradorRepository>();
builder.Services.AddScoped<IAutenticacionAdministradorService, AutenticacionAdministradorService>();

builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.UseCors("Angular");
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