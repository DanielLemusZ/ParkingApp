using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de los cobros a la tabla <c>cobros</c>.
/// </summary>
internal sealed class CobroConfiguracion : IEntityTypeConfiguration<Cobro>
{
    public void Configure(EntityTypeBuilder<Cobro> builder)
    {
        // Una tarifa de $0 no genera cobro, así que todo cobro guardado vale más que cero.
        builder.ToTable("cobros", tabla => tabla.HasCheckConstraint("ck_cobros_valor_positivo", "valor > 0"));

        builder.HasKey(cobro => cobro.Id);
        builder.Property(cobro => cobro.Id).ValueGeneratedNever();

        // La placa se guarda como texto: al guardar se usa placa.Valor, y al leer se vuelve a crear la Placa.
        builder.Property(cobro => cobro.Placa)
            .HasConversion(placa => placa.Valor, valor => Placa.Crear(valor))
            .HasMaxLength(Placa.LargoMaximo);

        // Los enums se guardan como texto, para que se entiendan al consultar la tabla.
        builder.Property(cobro => cobro.TipoDeVehiculo).HasConversion<string>().HasMaxLength(20);
        builder.Property(cobro => cobro.Franja).HasConversion<string>().HasMaxLength(10);
        builder.Property(cobro => cobro.Estado).HasConversion<string>().HasMaxLength(10);

        // Cada cobro pertenece a una sede y guarda la versión de tarifa con la que se calculó (RN-07).
        // Ni la sede ni una tarifa usada se pueden borrar.
        builder.HasOne<Sede>().WithMany().HasForeignKey(cobro => cobro.SedeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tarifa>().WithMany().HasForeignKey(cobro => cobro.TarifaId).OnDelete(DeleteBehavior.Restrict);

        // Busca el cobro del día de una placa. La historia #12 lo vuelve único (RN-04).
        builder.HasIndex(cobro => new { cobro.Placa, cobro.SedeId, cobro.DiaOperativo });
    }
}
