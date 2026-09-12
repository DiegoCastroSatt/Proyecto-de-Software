using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;

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

    public DbSet<Producto> Productos => Set<Producto>();

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
            entity.Property(h => h.Fecha).HasColumnName("fecha").HasColumnType("date");
            entity.Property(h => h.HoraInicio).HasColumnName("hora_inicio").HasColumnType("time");
            entity.Property(h => h.HoraFin).HasColumnName("hora_fin").HasColumnType("time");
            entity.Property(h => h.Estado).HasColumnName("estado");
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
            entity.HasIndex(p => p.Codigo).IsUnique();
        });
    }
}