using Microsoft.EntityFrameworkCore;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia;

/// <summary>
/// Contexto de EF Core de la base de datos del MVP.
/// </summary>
public sealed class ParkingAppDbContext : DbContext
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
    /// Cobros generados por los vehículos.
    /// </summary>
    public DbSet<Cobro> Cobros => Set<Cobro>();

    /// <summary>
    /// Sedes del parqueadero.
    /// </summary>
    public DbSet<Sede> Sedes => Set<Sede>();

    /// <summary>
    /// Ingresos y salidas de los vehículos.
    /// </summary>
    public DbSet<EventoDeAcceso> EventosDeAcceso => Set<EventoDeAcceso>();

    /// <summary>
    /// Versiones históricas de las tarifas.
    /// </summary>
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();

    /// <summary>
    /// Cobros de los vehículos, uno por placa, sede y día operativo.
    /// </summary>
    public DbSet<Cobro> Cobros => Set<Cobro>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Convenciones para todas las entidades: fechas y horas en UTC, dinero exacto y día operativo como fecha.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<decimal>().HaveColumnType("numeric(12,2)");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las clases de la carpeta Configuraciones (una por entidad).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingAppDbContext).Assembly);
    }
}
