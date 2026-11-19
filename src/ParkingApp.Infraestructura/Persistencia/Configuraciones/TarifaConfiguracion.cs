using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de las versiones de tarifas a la tabla <c>tarifas</c>.
/// </summary>
internal sealed class TarifaConfiguracion : IEntityTypeConfiguration<Tarifa>
{
    private static readonly DateOnly FechaInicial = new DateOnly(2026, 11, 1);

    private static readonly Guid IdCarroDiurno = new Guid("019a9000-0000-7000-8000-000000000001");

    private static readonly Guid IdCarroNocturno = new Guid("019a9000-0000-7000-8000-000000000002");

    private static readonly Guid IdMotoDiurno = new Guid("019a9000-0000-7000-8000-000000000003");

    private static readonly Guid IdMotoNocturno = new Guid("019a9000-0000-7000-8000-000000000004");

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

        builder.HasData(
            new
            {
                Id = IdCarroDiurno,
                TipoDeVehiculo = TipoDeVehiculo.Carro,
                Franja = FranjaTarifaria.Diurna,
                Valor = 8000m,
                VigenteDesde = FechaInicial,
                VigenteHasta = (DateOnly?)null,
            },
            new
            {
                Id = IdCarroNocturno,
                TipoDeVehiculo = TipoDeVehiculo.Carro,
                Franja = FranjaTarifaria.Nocturna,
                Valor = 5000m,
                VigenteDesde = FechaInicial,
                VigenteHasta = (DateOnly?)null,
            },
            new
            {
                Id = IdMotoDiurno,
                TipoDeVehiculo = TipoDeVehiculo.Moto,
                Franja = FranjaTarifaria.Diurna,
                Valor = 0m,
                VigenteDesde = FechaInicial,
                VigenteHasta = (DateOnly?)null,
            },
            new
            {
                Id = IdMotoNocturno,
                TipoDeVehiculo = TipoDeVehiculo.Moto,
                Franja = FranjaTarifaria.Nocturna,
                Valor = 0m,
                VigenteDesde = FechaInicial,
                VigenteHasta = (DateOnly?)null,
            });
    }
}
