using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de los pagos a la tabla <c>pagos</c>.
/// </summary>
internal sealed class PagoConfiguracion : IEntityTypeConfiguration<Pago>
{
    /// <summary>
    /// Nombre de la restricción única de la referencia del pago (idempotencia, F-04).
    /// </summary>
    internal const string IndiceUnicoDeReferencia = "ux_pagos_referencia";

    /// <summary>
    /// Nombre de la restricción única del cobro pagado: un cobro tiene a lo sumo un pago.
    /// </summary>
    internal const string IndiceUnicoDelCobro = "ux_pagos_cobro";

    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        // El pago copia el valor del cobro, que siempre es mayor que cero.
        builder.ToTable("pagos", tabla => tabla.HasCheckConstraint("ck_pagos_valor_positivo", "valor > 0"));

        builder.HasKey(pago => pago.Id);
        builder.Property(pago => pago.Id).ValueGeneratedNever();

        builder.Property(pago => pago.Medio).HasConversion<string>().HasMaxLength(20);
        builder.Property(pago => pago.Operador).HasMaxLength(Pago.LargoMaximoOperador);

        // Un cobro pagado no se puede borrar.
        builder.HasOne<Cobro>().WithMany().HasForeignKey(pago => pago.CobroId).OnDelete(DeleteBehavior.Restrict);

        // La misma solicitud de pago, repetida por un doble clic o un reintento, trae la misma referencia: la base de
        // datos deja guardar solo un pago, aunque las dos solicitudes lleguen a la vez.
        builder.HasIndex(pago => pago.Referencia).IsUnique().HasDatabaseName(IndiceUnicoDeReferencia);

        // Dos solicitudes con referencias distintas tampoco pueden pagar dos veces el mismo cobro: un cobro tiene a lo sumo un pago (F-04).
        builder.HasIndex(pago => pago.CobroId).IsUnique().HasDatabaseName(IndiceUnicoDelCobro);
    }
}
