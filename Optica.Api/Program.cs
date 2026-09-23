using Optica.Api.Modules.Ventas.Interfaces;
using Optica.Api.Modules.Ventas.Repositories;
using Optica.Api.Modules.Ventas.Services;
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
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var app = builder.Build();

Exception? ultimoError = null;
for (var intento = 1; intento <= 10; intento++)
{
    try
    {
        using var scope = app.Services.CreateScope();
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
            CREATE TABLE IF NOT EXISTS pedidos (
                id_pedido INT AUTO_INCREMENT PRIMARY KEY,
                rut VARCHAR(12) NOT NULL,
                id_receta INT NULL,
                fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                estado ENUM('Pendiente', 'En proceso', 'Listo', 'Entregado', 'Cancelado') NOT NULL DEFAULT 'Pendiente',
                total DECIMAL(10,2) NOT NULL DEFAULT 0,
                anotaciones TEXT,
                CONSTRAINT fk_pedidos_cliente FOREIGN KEY (rut) REFERENCES clientes(rut) ON DELETE CASCADE,
                CONSTRAINT fk_pedidos_receta FOREIGN KEY (id_receta) REFERENCES recetas(id_receta) ON DELETE SET NULL
            )
            """);

        db.Database.ExecuteSqlRaw("""
            INSERT IGNORE INTO catalogos (tipo, nombre) VALUES
            ('Marca', 'Ray-Ban'), ('Marca', 'Oakley'), ('Marca', 'Vogue'), ('Marca', 'Polaroid'),
            ('Color', 'Negro'), ('Color', 'Café'), ('Color', 'Dorado'), ('Color', 'Plateado'), ('Color', 'Transparente'),
            ('Categoria', 'Lentes ópticos'), ('Categoria', 'Lentes de sol'), ('Categoria', 'Armazones'),
            ('Categoria', 'Lentes de contacto'), ('Categoria', 'Accesorios')
            """);

        var rutaImagenExiste = await db.Database
            .SqlQueryRaw<int>("""
                SELECT COUNT(*) AS Value
                FROM information_schema.columns
                WHERE table_schema = DATABASE()
                  AND table_name = 'productos'
                  AND column_name = 'ruta_imagen'
                """)
            .SingleAsync();

        if (rutaImagenExiste == 0)
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE productos ADD COLUMN ruta_imagen VARCHAR(255) NULL");
        }

        ultimoError = null;
        break;
    }
    catch (Exception ex) when (intento < 10)
    {
        ultimoError = ex;
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

if (ultimoError is not null)
{
    throw new InvalidOperationException("No se pudo inicializar la base de datos.", ultimoError);
}

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
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