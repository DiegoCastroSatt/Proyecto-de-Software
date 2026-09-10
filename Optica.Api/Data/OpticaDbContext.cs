using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.Appointments.Models;
using Optica.Api.Modules.Customers.Models;

namespace Optica.Api.Data;

public class OpticaDbContext : DbContext
{
    public OpticaDbContext(DbContextOptions<OpticaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("clientes");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id_cliente");

            entity.Property(c => c.NationalId)
                .HasColumnName("rut")
                .HasMaxLength(12)
                .IsRequired();

            entity.Property(c => c.FirstName)
                .HasColumnName("nombre")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(c => c.LastName)
                .HasColumnName("apellido")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(c => c.Phone)
                .HasColumnName("telefono")
                .HasMaxLength(20);

            entity.Property(c => c.Email)
                .HasColumnName("correo")
                .HasMaxLength(100);

            entity.Property(c => c.Status)
                .HasColumnName("estado")
                .HasMaxLength(8)
                .IsRequired();

            entity.Property(c => c.RegisteredAt)
                .HasColumnName("fecha_registro");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("reservas");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .HasColumnName("id_reserva");

            entity.Property(r => r.CustomerId)
                .HasColumnName("id_cliente");

            entity.Property(r => r.Date)
                .HasColumnName("fecha")
                .HasColumnType("date");

            entity.Property(r => r.Time)
                .HasColumnName("hora")
                .HasColumnType("time");

            entity.Property(r => r.Status)
                .HasColumnName("estado");

            entity.Ignore(r => r.CreatedAt);
        });
    }
}
