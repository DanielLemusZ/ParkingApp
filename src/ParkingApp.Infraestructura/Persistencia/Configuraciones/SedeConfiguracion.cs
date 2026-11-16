using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de la sede a la tabla <c>sedes</c>, con la sede del MVP como dato semilla.
/// </summary>
internal sealed class SedeConfiguracion : IEntityTypeConfiguration<Sede>
{
    /// <summary>
    /// Identificador fijo de la sede del MVP.
    /// </summary>
    internal static readonly Guid IdSedeDelMvp = new Guid("019a8000-0000-7000-8000-000000000001");

    /// <summary>
    /// Zona horaria de la sede del MVP.
    /// </summary>
    internal const string ZonaHorariaDeLaSedeDelMvp = "America/Bogota";

    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.HasKey(sede => sede.Id);
        builder.Property(sede => sede.Id).ValueGeneratedNever();
        builder.Property(sede => sede.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(sede => sede.ZonaHoraria).HasMaxLength(64).IsRequired();

        // El dato semilla se escribe como un objeto anónimo (new { ... }) porque el constructor de Sede
        // crea un Id nuevo cada vez, y la semilla necesita siempre el mismo Id.
        builder.HasData(new
        {
            Id = IdSedeDelMvp,
            Nombre = "Sede principal UAM",
            ZonaHoraria = ZonaHorariaDeLaSedeDelMvp,
        });
    }
}
