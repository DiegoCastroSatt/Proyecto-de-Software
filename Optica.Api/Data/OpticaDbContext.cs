using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.RegistroRecetas.Models;

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
    }
}