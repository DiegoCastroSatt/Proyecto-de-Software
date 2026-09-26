using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Catalogos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;
using Optica.Api.Modules.RegistroRecetas.Models;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Pedidos.Models;

namespace Optica.Api.Data;

public class OpticaDbContext : DbContext
{
    public OpticaDbContext(DbContextOptions<OpticaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Reserva> Reservas => Set<Reserva>();

    public DbSet<Horario> Horarios => Set<Horario>();

    public DbSet<Receta> Recetas => Set<Receta>();

    public DbSet<Graduacion> Graduaciones => Set<Graduacion>();

    public DbSet<Administrador> Administradores => Set<Administrador>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<CatalogoItem> Catalogos => Set<CatalogoItem>();

    public DbSet<Venta> Ventas => Set<Venta>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");

            entity.HasKey(c => c.IdCliente);

            entity.Property(c => c.IdCliente)
                .HasColumnName("id_cliente");

            entity.Property(c => c.Rut)
                .HasColumnName("rut")
                .HasMaxLength(12)
                .IsRequired();

            entity.Property(c => c.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(c => c.Apellido)
                .HasColumnName("apellido")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(c => c.Telefono)
                .HasColumnName("telefono")
                .HasMaxLength(20);

            entity.Property(c => c.Correo)
                .HasColumnName("correo")
                .HasMaxLength(100);

            entity.Property(c => c.Estado)
                .HasColumnName("estado")
                .HasMaxLength(8)
                .IsRequired();

            entity.Property(c => c.FechaRegistro)
                .HasColumnName("fecha_registro");
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("reservas");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .HasColumnName("id_reserva");

            entity.Property(r => r.ClienteId)
                .HasColumnName("id_cliente");

            entity.Property(r => r.HorarioId)
                .HasColumnName("id_horario");

            entity.Ignore(r => r.Fecha);
            entity.Ignore(r => r.Hora);

            entity.Property(r => r.Estado)
                .HasColumnName("estado");

        });

        modelBuilder.Entity<Horario>(entity =>
        {
            entity.ToTable("horarios_atencion");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id_horario");
            entity.Property(h => h.AdministradorId).HasColumnName("id_administrador");
            entity.Property(h => h.Fecha).HasColumnName("fecha").HasColumnType("date");
            entity.Property(h => h.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time");
            entity.Property(h => h.HoraFin).HasColumnName("hora_fin").HasColumnType("time");
            entity.Property(h => h.Estado).HasColumnName("estado");
        });

        modelBuilder.Entity<Receta>(entity =>
        {
            entity.ToTable("recetas");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .HasColumnName("id_receta");

            entity.Property(r => r.ClienteId)
                .HasColumnName("id_cliente");

            entity.Property(r => r.Fecha)
                .HasColumnName("fecha")
                .HasColumnType("date");

            entity.Property(r => r.Observaciones)
                .HasColumnName("observaciones");

            entity.Property(r => r.ImagenPath)
                .HasColumnName("imagen_path")
                .HasMaxLength(255);

            entity.Ignore(r => r.FechaCreacion);
        });

        modelBuilder.Entity<Graduacion>(entity =>
        {
            entity.ToTable("graduaciones");

            entity.HasKey(g => g.Id);

            entity.Property(g => g.Id)
                .HasColumnName("id_graduacion");

            entity.Property(g => g.RecetaId)
                .HasColumnName("id_receta");

            entity.Property(g => g.Ojo)
                .HasColumnName("ojo")
                .HasMaxLength(2)
                .IsRequired();

            entity.Property(g => g.Esfera)
                .HasColumnName("esfera")
                .HasColumnType("decimal(4,2)");

            entity.Property(g => g.Cilindro)
                .HasColumnName("cilindro")
                .HasColumnType("decimal(4,2)");

            entity.Property(g => g.Eje)
                .HasColumnName("eje");

            entity.Property(g => g.Adicion)
                .HasColumnName("adicion")
                .HasColumnType("decimal(4,2)");

            entity.HasOne<Receta>()
                .WithMany(r => r.Graduaciones)
                .HasForeignKey(g => g.RecetaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Administrador>(entity =>
        {
            entity.ToTable("administradores");
            entity.HasKey(a => a.IdAdministrador);
            entity.Property(a => a.IdAdministrador).HasColumnName("id_administrador");
            entity.Property(a => a.Nombre).HasColumnName("nombre");
            entity.Property(a => a.Correo).HasColumnName("correo");
            entity.Property(a => a.Contrasena).HasColumnName("contrasena");
            entity.Property(a => a.Estado).HasColumnName("estado");
            entity.HasIndex(a => a.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.IdProducto);
            entity.Property(p => p.IdProducto).HasColumnName("id_producto");
            entity.Property(p => p.Codigo).HasColumnName("codigo").HasMaxLength(30).IsRequired();
            entity.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            entity.Property(p => p.Marca).HasColumnName("marca").HasMaxLength(60);
            entity.Property(p => p.Modelo).HasColumnName("modelo").HasMaxLength(60);
            entity.Property(p => p.Color).HasColumnName("color").HasMaxLength(40);
            entity.Property(p => p.Categoria).HasColumnName("categoria").HasMaxLength(50);
            entity.Property(p => p.Precio).HasColumnName("precio").HasPrecision(10, 2).IsRequired();
            entity.Property(p => p.Stock).HasColumnName("stock").IsRequired();
            entity.Property(p => p.StockMinimo).HasColumnName("stock_minimo").IsRequired();
            entity.Property(p => p.Estado).HasColumnName("estado").HasMaxLength(9).IsRequired();
            entity.Property(p => p.RutaImagen).HasColumnName("ruta_imagen").HasMaxLength(255);
            entity.HasIndex(p => p.Codigo).IsUnique();
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("ventas");
            entity.HasKey(v => v.IdVenta);
            entity.Property(v => v.IdVenta).HasColumnName("id_venta");
            entity.Property(v => v.Fecha).HasColumnName("fecha");
            entity.Property(v => v.Total).HasColumnName("total").HasPrecision(10, 2);
            entity.HasMany(v => v.Detalles).WithOne().HasForeignKey(d => d.VentaId);
        });

        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.ToTable("detalle_venta");
            entity.HasKey(d => d.IdDetalle);
            entity.Property(d => d.IdDetalle).HasColumnName("id_detalle");
            entity.Property(d => d.VentaId).HasColumnName("id_venta");
            entity.Property(d => d.ProductoId).HasColumnName("id_producto").IsRequired(false);
            entity.Property(d => d.NombreProducto).HasColumnName("nombre_producto").HasMaxLength(100).IsRequired();
            entity.HasOne<Producto>().WithMany().HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(d => d.Cantidad).HasColumnName("cantidad");
            entity.Property(d => d.PrecioUnitario).HasColumnName("precio_unitario").HasPrecision(10, 2);
            entity.Property(d => d.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2);
        });

        modelBuilder.Entity<CatalogoItem>(entity =>
        {
            entity.ToTable("catalogos");
            entity.HasKey(c => c.IdCatalogo);
            entity.Property(c => c.IdCatalogo).HasColumnName("id_catalogo");
            entity.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();
            entity.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(60).IsRequired();
            entity.HasIndex(c => new { c.Tipo, c.Nombre }).IsUnique();
        });
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("pedidos");
            entity.HasKey(p => p.IdPedido);
            entity.Property(p => p.IdPedido).HasColumnName("id_pedido");
            entity.Property(p => p.Rut).HasColumnName("rut");
            entity.Property(p => p.IdReceta).HasColumnName("id_receta");
            entity.Property(p => p.Fecha).HasColumnName("fecha");
            entity.Property(p => p.Estado).HasColumnName("estado");
            entity.Property(p => p.Total).HasColumnName("total").HasPrecision(10, 2);
            entity.Property(p => p.Anotaciones).HasColumnName("anotaciones");
        });
    }
}