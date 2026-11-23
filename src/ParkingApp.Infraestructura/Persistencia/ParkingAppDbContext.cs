using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Infraestructura.Identidad;

namespace ParkingApp.Infraestructura.Persistencia;

/// <summary>
/// Contexto de EF Core de la base de datos del MVP. Hereda de <see cref="IdentityDbContext{TUser, TRole, TKey}"/>
/// para que las tablas de usuarios y roles de Identity vivan en la misma base de datos y en las mismas migraciones.
/// </summary>
public sealed class ParkingAppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Crea el contexto con las opciones registradas al arrancar (cadena de conexión y PostgreSQL).
    /// </summary>
    /// <param name="opciones">Opciones de EF Core.</param>
    public ParkingAppDbContext(DbContextOptions<ParkingAppDbContext> opciones)
        : base(opciones)
    {
    }

    /// <summary>
    /// Sedes del parqueadero.
    /// </summary>
    public DbSet<Sede> Sedes => Set<Sede>();

    /// <summary>
    /// Ingresos y salidas de los vehículos.
    /// </summary>
    public DbSet<EventoDeAcceso> EventosDeAcceso => Set<EventoDeAcceso>();

    /// <summary>
    /// Lecturas que valida el operador en la caja, pendientes o ya validadas.
    /// </summary>
    public DbSet<LecturaPendiente> LecturasPendientes => Set<LecturaPendiente>();

    /// <summary>
    /// Versiones históricas de las tarifas.
    /// </summary>
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();

    /// <summary>
    /// Cobros de los vehículos, uno por placa, sede y día operativo.
    /// </summary>
    public DbSet<Cobro> Cobros => Set<Cobro>();

    /// <summary>
    /// Pagos de los cobros, cada uno con su referencia única.
    /// </summary>
    public DbSet<Pago> Pagos => Set<Pago>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Convenciones para todas las entidades: fechas y horas en UTC, dinero exacto y día operativo como fecha.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<decimal>().HaveColumnType("numeric(12,2)");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Primero Identity define sus tablas (usuarios, roles, claims, logins y tokens). Si se omite, EF no sabe
        // cuál es la llave de esas tablas y la migración falla.
        base.OnModelCreating(builder);

        // Identity fija los nombres de sus tablas en PascalCase ("AspNetUsers"), y la convención snake_case solo cambia
        // los nombres que nadie fijó. Se renombran para que toda la base use el mismo estilo.
        builder.Entity<Usuario>().ToTable("asp_net_users");
        builder.Entity<IdentityRole<Guid>>().ToTable("asp_net_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("asp_net_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("asp_net_role_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");

        // Después se aplican todas las clases de la carpeta Configuraciones (una por entidad), incluida la semilla de roles.
        builder.ApplyConfigurationsFromAssembly(typeof(ParkingAppDbContext).Assembly);
    }
}
