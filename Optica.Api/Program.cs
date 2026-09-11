using Microsoft.EntityFrameworkCore;
using Optica.Api.Data;

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
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Implementación temporal hasta configurar la base de datos.
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
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
