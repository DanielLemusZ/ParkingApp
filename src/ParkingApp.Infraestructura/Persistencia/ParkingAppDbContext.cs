using Microsoft.EntityFrameworkCore;

using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia;

/// <summary>
/// Contexto de EF Core de la base de datos del MVP.
/// </summary>
public sealed class ParkingAppDbContext(DbContextOptions<ParkingAppDbContext> opciones) : DbContext(opciones)
{
    /// <summary>
    /// Sedes del parqueadero.
    /// </summary>
    public DbSet<Sede> Sedes => Set<Sede>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Convenciones para todas las entidades: fechas y horas en UTC, dinero exacto y día operativo como fecha.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<decimal>().HaveColumnType("numeric(12,2)");
        configurationBuilder.Properties<DateOnly>().HaveColumnType("date");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingAppDbContext).Assembly);
}
