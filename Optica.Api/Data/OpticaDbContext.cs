using Optica.Api.Modules.Rostros;
using Optica.Api.Modules.Marcos;
using Optica.Api.Modules.Accesos;
using Microsoft.EntityFrameworkCore;
using Optica.Api.Modules.AgendaReservas.Models;
using Optica.Api.Modules.Catalogos.Models;
using Optica.Api.Modules.Clientes.Models;
using Optica.Api.Modules.Productos.Models;
using Optica.Api.Modules.RegistroRecetas.Models;
using Optica.Api.Modules.Ventas.Models;
using Optica.Api.Modules.Pedidos.Models;
using Optica.Api.Modules.Resenas.Models;
using Optica.Api.Modules.Autenticacion.Models;

namespace Optica.Api.Data;

public class OpticaDbContext : DbContext
{
    public OpticaDbContext(DbContextOptions<OpticaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<CuentaCliente> CuentasCliente => Set<CuentaCliente>();

    public DbSet<Reserva> Reservas => Set<Reserva>();

    public DbSet<Horario> Horarios => Set<Horario>();

    public DbSet<Receta> Recetas => Set<Receta>();

    public DbSet<Graduacion> Graduaciones => Set<Graduacion>();

    public DbSet<Administrador> Administradores => Set<Administrador>();

    public DbSet<TipoRostro> TiposRostro => Set<TipoRostro>();

    public DbSet<FormaLente> FormasLente => Set<FormaLente>();

    public DbSet<TipoMarco> TiposMarco => Set<TipoMarco>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<CatalogoItem> Catalogos => Set<CatalogoItem>();

    public DbSet<Venta> Ventas => Set<Venta>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<CorreoReservaPendiente> CorreosReserva => Set<CorreoReservaPendiente>();

    public DbSet<IntentoCorreoReserva> IntentosCorreoReserva => Set<IntentoCorreoReserva>();

    public DbSet<CorreoPedidoPendiente> CorreosPedido => Set<CorreoPedidoPendiente>();

    public DbSet<IntentoCorreoPedido> IntentosCorreoPedido => Set<IntentoCorreoPedido>();


    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    public DbSet<Trabajador> Trabajadores => Set<Trabajador>();
    public DbSet<RolTrabajador> RolesTrabajadores => Set<RolTrabajador>();
    public DbSet<Resena> Resenas => Set<Resena>();
    public DbSet<DesafioRecuperacion> DesafiosRecuperacion => Set<DesafioRecuperacion>();
    public DbSet<LimiteRecuperacion> LimitesRecuperacion => Set<LimiteRecuperacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<FormaLente>().HasIndex(f => f.Codigo).IsUnique();
        modelBuilder.Entity<FormaLente>().HasIndex(f => f.NombreNormalizado).IsUnique();
        modelBuilder.Entity<DesafioRecuperacion>(e =>
        {
            e.ToTable("desafios_recuperacion", table =>
            {
                table.HasCheckConstraint("ck_desafio_tipo_cuenta", "(tipo_cuenta = 'Cliente' AND id_cuenta_cliente IS NOT NULL AND id_personal IS NULL) OR (tipo_cuenta IN ('Administrador','Trabajador') AND id_personal IS NOT NULL AND id_cuenta_cliente IS NULL)");
                table.HasCheckConstraint("ck_desafio_intentos", "intentos BETWEEN 0 AND 5");
                table.HasCheckConstraint("ck_desafio_estado", "estado IN ('Pendiente','Usado','Invalidado','Agotado','Expirado')");
            });
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id_desafio");
            e.Property(d => d.TipoCuenta).HasColumnName("tipo_cuenta").HasMaxLength(20).IsRequired();
            e.Property(d => d.IdCuentaCliente).HasColumnName("id_cuenta_cliente");
            e.Property(d => d.IdPersonal).HasColumnName("id_personal");
            e.Property(d => d.HashCodigo).HasColumnName("hash_codigo").HasMaxLength(64).IsRequired();
            e.Property(d => d.CreadoUtc).HasColumnName("creado_utc");
            e.Property(d => d.ExpiraUtc).HasColumnName("expira_utc");
            e.Property(d => d.Intentos).HasColumnName("intentos");
            e.Property(d => d.Estado).HasColumnName("estado").HasMaxLength(16).IsRequired();
            e.Property(d => d.HuellaIdentificador).HasColumnName("huella_identificador").HasMaxLength(64).IsRequired();
            e.Property(d => d.HuellaIp).HasColumnName("huella_ip").HasMaxLength(64).IsRequired();
            e.Property(d => d.UltimaSolicitudUtc).HasColumnName("ultima_solicitud_utc");
            e.Property<int?>("IdCuentaClientePendiente").HasColumnName("id_cuenta_cliente_pendiente").HasComputedColumnSql("CASE WHEN estado = 'Pendiente' THEN id_cuenta_cliente ELSE NULL END", stored: true);
            e.Property<int?>("IdPersonalPendiente").HasColumnName("id_personal_pendiente").HasComputedColumnSql("CASE WHEN estado = 'Pendiente' THEN id_personal ELSE NULL END", stored: true);
            e.HasIndex("IdCuentaClientePendiente").IsUnique();
            e.HasIndex("IdPersonalPendiente").IsUnique();
            e.HasIndex(d => new { d.HuellaIdentificador, d.CreadoUtc });
            e.HasIndex(d => new { d.HuellaIp, d.CreadoUtc });
            e.HasOne<CuentaCliente>().WithMany().HasForeignKey(d => d.IdCuentaCliente).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Trabajador>().WithMany().HasForeignKey(d => d.IdPersonal).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<LimiteRecuperacion>(e =>
        {
            e.ToTable("limites_recuperacion");
            e.HasKey(l => l.Huella);
            e.Property(l => l.Huella).HasColumnName("huella").HasMaxLength(64);
            e.Property(l => l.InicioVentanaUtc).HasColumnName("inicio_ventana_utc");
            e.Property(l => l.Solicitudes).HasColumnName("solicitudes");
            e.Property(l => l.UltimaSolicitudUtc).HasColumnName("ultima_solicitud_utc");
        });
        modelBuilder.Entity<TipoRostro>().HasIndex(r => r.Codigo).IsUnique();
        modelBuilder.Entity<TipoRostro>().HasIndex(r => r.Nombre).IsUnique();
        modelBuilder.Entity<TipoMarco>().HasIndex(t => t.Codigo).IsUnique();
        modelBuilder.Entity<TipoMarco>().HasIndex(t => t.Nombre).IsUnique();
        modelBuilder.Entity<RolTrabajador>(e =>
        {
            e.ToTable("roles_trabajadores"); e.HasKey(r => r.IdRol);
            e.Property(r => r.IdRol).HasColumnName("id_rol");
            e.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(60).IsRequired();
            e.Property(r => r.PermisosJson).HasColumnName("permisos_json").HasColumnType("longtext").IsRequired();
            e.HasIndex(r => r.Nombre).IsUnique();
        });
        modelBuilder.Entity<Trabajador>(e =>
        {
            e.ToTable("trabajador_optica"); e.HasKey(t => t.IdTrabajador);
            e.Property(t => t.IdTrabajador).HasColumnName("id_trabajador");
            e.Property(t => t.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(t => t.NombreUsuario).HasColumnName("nombre_usuario").HasMaxLength(60).IsRequired();
            e.Property(t => t.Correo).HasColumnName("correo").HasMaxLength(100).IsRequired();
            e.Property(t => t.Contrasena).HasColumnName("contrasena").HasMaxLength(255).IsRequired();
            e.Property(t => t.Estado).HasColumnName("estado").HasMaxLength(8).IsRequired();
            e.Property(t => t.IdRol).HasColumnName("id_rol");
            e.Property(t => t.IdAdministradorCreador).HasColumnName("id_administrador_creador");
            e.HasIndex(t => t.NombreUsuario).IsUnique();
            e.HasDiscriminator<string>("TipoUsuario").HasValue<Trabajador>("Trabajador").HasValue<Administrador>("Administrador");
            e.Property<string>("TipoUsuario").HasColumnName("tipo_usuario").HasMaxLength(20);
            e.HasOne(t => t.Rol).WithMany().HasForeignKey(t => t.IdRol).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Administrador>().WithMany().HasForeignKey(t => t.IdAdministradorCreador).OnDelete(DeleteBehavior.Restrict);
        });

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

        modelBuilder.Entity<CuentaCliente>(entity =>
        {
            entity.ToTable("cuentas_cliente");
            entity.HasKey(c => c.IdCuenta);
            entity.Property(c => c.IdCuenta).HasColumnName("id_cuenta");
            entity.Property(c => c.IdCliente).HasColumnName("id_cliente");
            entity.Property(c => c.Correo).HasColumnName("correo").HasMaxLength(100).IsRequired();
            entity.Property(c => c.CorreoNormalizado).HasColumnName("correo_normalizado").HasMaxLength(100).IsRequired();
            entity.Property(c => c.ContrasenaHash).HasColumnName("contrasena_hash").HasMaxLength(500).IsRequired();
            entity.Property(c => c.Estado).HasColumnName("estado").HasMaxLength(24).IsRequired();
            entity.Property(c => c.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(c => c.TokenConfirmacionHash).HasColumnName("token_confirmacion_hash").HasMaxLength(64);
            entity.Property(c => c.TokenConfirmacionExpiraUtc).HasColumnName("token_confirmacion_expira_utc");
            entity.Property(c => c.UltimoCorreoConfirmacionUtc).HasColumnName("ultimo_correo_confirmacion_utc");
            entity.HasIndex(c => c.IdCliente).IsUnique();
            entity.HasIndex(c => c.CorreoNormalizado).IsUnique();
            entity.HasOne(c => c.Cliente)
                .WithOne()
                .HasForeignKey<CuentaCliente>(c => c.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.Property(r => r.Motivo)
                .HasColumnName("motivo")
                .HasMaxLength(150);

            entity.Property(r => r.TokenConfirmacionHash)
                .HasColumnName("token_confirmacion_hash")
                .HasMaxLength(64);
            entity.Property(r => r.TokenCancelacionHash)
                .HasColumnName("token_cancelacion_hash")
                .HasMaxLength(64);
            entity.Property(r => r.TokenAccionExpiraUtc)
                .HasColumnName("token_accion_expira_utc");
            entity.Property(r => r.RecordatorioEnviadoUtc)
                .HasColumnName("recordatorio_enviado_utc");
            entity.HasIndex(r => r.TokenConfirmacionHash).IsUnique();
            entity.HasIndex(r => r.TokenCancelacionHash).IsUnique();
            entity.HasIndex(r => new { r.Estado, r.RecordatorioEnviadoUtc });

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


        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.IdProducto);
            entity.Property(p => p.IdProducto).HasColumnName("id_producto");
            entity.Property(p => p.IdTipoMarco).HasColumnName("id_tipo_marco");
            entity.HasOne<TipoMarco>().WithMany().HasForeignKey(p => p.IdTipoMarco).OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(v => v.ClienteId).HasColumnName("id_cliente");
            entity.HasOne(v => v.Cliente).WithMany().HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<CorreoReservaPendiente>(entity =>
        {
            entity.ToTable("correos_reservas");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id_correo");
            entity.Property(c => c.ReservaId).HasColumnName("id_reserva");
            entity.Property(c => c.HorarioId).HasColumnName("id_horario");
            entity.Property(c => c.Tipo).HasColumnName("tipo").HasMaxLength(30).IsRequired();
            entity.Property(c => c.Destinatario).HasColumnName("destinatario").HasMaxLength(254).IsRequired();
            entity.Property(c => c.Asunto).HasColumnName("asunto").HasMaxLength(255).IsRequired();
            entity.Property(c => c.CuerpoHtmlProtegido).HasColumnName("cuerpo_html_protegido").HasColumnType("longtext").IsRequired();
            entity.Property(c => c.Adjunto).HasColumnName("adjunto").HasColumnType("longblob");
            entity.Property(c => c.NombreAdjunto).HasColumnName("nombre_adjunto").HasMaxLength(255);
            entity.Property(c => c.TipoAdjunto).HasColumnName("tipo_adjunto").HasMaxLength(100);
            entity.Property(c => c.Enviado).HasColumnName("enviado");
            entity.Property(c => c.Intentos).HasColumnName("intentos");
            entity.Property(c => c.CreadoUtc).HasColumnName("creado_utc");
            entity.Property(c => c.ProximoIntentoUtc).HasColumnName("proximo_intento_utc");
            entity.Property(c => c.ReclamadoHastaUtc).HasColumnName("reclamado_hasta_utc");
            entity.HasIndex(c => new { c.Enviado, c.ProximoIntentoUtc });
            entity.HasMany(c => c.Historial).WithOne().HasForeignKey(i => i.CorreoReservaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntentoCorreoReserva>(entity =>
        {
            entity.ToTable("intentos_correo_reservas");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasColumnName("id_intento");
            entity.Property(i => i.CorreoReservaId).HasColumnName("id_correo");
            entity.Property(i => i.FechaUtc).HasColumnName("fecha_utc");
            entity.Property(i => i.Exitoso).HasColumnName("exitoso");
            entity.Property(i => i.Error).HasColumnName("error").HasColumnType("text");
        });

        modelBuilder.Entity<CorreoPedidoPendiente>(entity =>
        {
            entity.ToTable("correos_pedidos");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id_correo");
            entity.Property(c => c.PedidoId).HasColumnName("id_pedido");
            entity.Property(c => c.Destinatario).HasColumnName("destinatario").HasMaxLength(254).IsRequired();
            entity.Property(c => c.Asunto).HasColumnName("asunto").HasMaxLength(255).IsRequired();
            entity.Property(c => c.CuerpoHtml).HasColumnName("cuerpo_html").HasColumnType("longtext").IsRequired();
            entity.Property(c => c.Importante).HasColumnName("importante");
            entity.Property(c => c.Enviado).HasColumnName("enviado");
            entity.Property(c => c.Intentos).HasColumnName("intentos");
            entity.Property(c => c.CreadoUtc).HasColumnName("creado_utc");
            entity.Property(c => c.ProximoIntentoUtc).HasColumnName("proximo_intento_utc");
            entity.Property(c => c.ReclamadoHastaUtc).HasColumnName("reclamado_hasta_utc");
            entity.HasIndex(c => new { c.Enviado, c.ProximoIntentoUtc });
            entity.HasMany(c => c.Historial).WithOne().HasForeignKey(i => i.CorreoPedidoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntentoCorreoPedido>(entity =>
        {
            entity.ToTable("intentos_correo_pedidos");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasColumnName("id_intento");
            entity.Property(i => i.CorreoPedidoId).HasColumnName("id_correo");
            entity.Property(i => i.FechaUtc).HasColumnName("fecha_utc");
            entity.Property(i => i.Exitoso).HasColumnName("exitoso");
            entity.Property(i => i.Error).HasColumnName("error").HasColumnType("text");
        });

        modelBuilder.Entity<Resena>(entity =>
        {
            entity.ToTable("resenas_productos");
            entity.HasKey(r => r.IdResena);
            entity.Property(r => r.IdResena).HasColumnName("id_resena");
            entity.Property(r => r.IdCliente).HasColumnName("id_cliente");
            entity.Property(r => r.IdProducto).HasColumnName("id_producto");
            entity.Property(r => r.Calificacion).HasColumnName("calificacion");
            entity.Property(r => r.Comentario).HasColumnName("comentario").HasMaxLength(1000).IsRequired();
            entity.Property(r => r.FechaCreacionUtc).HasColumnName("fecha_creacion_utc");
            entity.Property(r => r.FechaActualizacionUtc).HasColumnName("fecha_actualizacion_utc");
            entity.HasIndex(r => new { r.IdCliente, r.IdProducto }).IsUnique();
            entity.HasOne(r => r.Cliente).WithMany().HasForeignKey(r => r.IdCliente).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Producto).WithMany().HasForeignKey(r => r.IdProducto).OnDelete(DeleteBehavior.Restrict);
        });

    }
}
