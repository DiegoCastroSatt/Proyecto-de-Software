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
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpticaDbContext>();
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS catalogos (
            id_catalogo INT AUTO_INCREMENT PRIMARY KEY,
            tipo VARCHAR(20) NOT NULL,
            nombre VARCHAR(60) NOT NULL,
            UNIQUE KEY uq_catalogo_tipo_nombre (tipo, nombre)
        )
        """);

    db.Database.ExecuteSqlRaw("""
        INSERT IGNORE INTO catalogos (tipo, nombre) VALUES
        ('Marca', 'Ray-Ban'), ('Marca', 'Oakley'), ('Marca', 'Vogue'), ('Marca', 'Polaroid'),
        ('Color', 'Negro'), ('Color', 'Café'), ('Color', 'Dorado'), ('Color', 'Plateado'), ('Color', 'Transparente'),
        ('Categoria', 'Lentes ópticos'), ('Categoria', 'Lentes de sol'), ('Categoria', 'Armazones'),
        ('Categoria', 'Lentes de contacto'), ('Categoria', 'Accesorios')
        """);
}

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
