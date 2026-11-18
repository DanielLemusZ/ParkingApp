using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de las versiones de tarifas a la tabla <c>tarifas</c>.
/// </summary>
internal sealed class TarifaConfiguracion : IEntityTypeConfiguration<Tarifa>
{
    public void Configure(EntityTypeBuilder<Tarifa> builder)
    {
        builder.ToTable("tarifas", tabla =>
        {
            tabla.HasCheckConstraint("ck_tarifas_valor_no_negativo", "valor >= 0");
            tabla.HasCheckConstraint("ck_tarifas_vigencia", "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
        });

        builder.HasKey(tarifa => tarifa.Id);
        builder.Property(tarifa => tarifa.Id).ValueGeneratedNever();
        builder.Property(tarifa => tarifa.TipoDeVehiculo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(tarifa => tarifa.Franja).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(tarifa => tarifa.Valor).IsRequired();
        builder.Property(tarifa => tarifa.VigenteDesde).IsRequired();
        builder.HasIndex(tarifa => new { tarifa.TipoDeVehiculo, tarifa.Franja, tarifa.VigenteDesde }).IsUnique();
        builder.HasIndex(tarifa => new { tarifa.TipoDeVehiculo, tarifa.Franja, tarifa.VigenteHasta });
    }
}
