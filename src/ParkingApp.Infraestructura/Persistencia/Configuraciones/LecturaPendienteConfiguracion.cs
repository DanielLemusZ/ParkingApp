using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de las lecturas que valida el operador a la tabla <c>lecturas_pendientes</c> (F-06).
/// </summary>
internal sealed class LecturaPendienteConfiguracion : IEntityTypeConfiguration<LecturaPendiente>
{
    public void Configure(EntityTypeBuilder<LecturaPendiente> builder)
    {
        // La base de datos también protege los rangos y que una lectura validada diga qué placa, quién y cuándo.
        builder.ToTable("lecturas_pendientes", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_lecturas_pendientes_confianza",
                $"confianza BETWEEN {EventoDeAcceso.ConfianzaMinima} AND {EventoDeAcceso.ConfianzaMaxima}");
            tabla.HasCheckConstraint("ck_lecturas_pendientes_carril", "carril > 0");
            tabla.HasCheckConstraint(
                "ck_lecturas_pendientes_validacion",
                "estado = 'Pendiente' OR (placa_validada IS NOT NULL AND validada_por IS NOT NULL AND fecha_de_validacion IS NOT NULL)");
        });

        builder.HasKey(lectura => lectura.Id);
        builder.Property(lectura => lectura.Id).ValueGeneratedNever();

        // Los enums se guardan como texto, para que se entiendan al consultar la tabla.
        builder.Property(lectura => lectura.Tipo).HasConversion<string>().HasMaxLength(10);
        builder.Property(lectura => lectura.Motivo).HasConversion<string>().HasMaxLength(30);

        // Al guardar, EF agrega "AND estado = <el que se leyó>" al UPDATE. Si otro operador validó primero,
        // no cambia ninguna fila y el repositorio lo informa: una lectura se valida una sola vez.
        builder.Property(lectura => lectura.Estado).HasConversion<string>().HasMaxLength(10).IsConcurrencyToken();

        // Las placas se guardan como texto. EF no llama la conversión cuando la placa validada está vacía (null).
        builder.Property(lectura => lectura.PlacaLeida)
            .HasConversion(placa => placa.Valor, valor => Placa.Crear(valor))
            .HasMaxLength(Placa.LargoMaximo);
        builder.Property(lectura => lectura.PlacaValidada)
            .HasConversion(placa => placa!.Valor, valor => Placa.Crear(valor))
            .HasMaxLength(Placa.LargoMaximo);

        builder.Property(lectura => lectura.ValidadaPor).HasMaxLength(EventoDeAcceso.LargoMaximoOperador);

        // Cada lectura pertenece a una sede existente, y una sede con lecturas no se puede borrar.
        builder.HasOne<Sede>().WithMany().HasForeignKey(lectura => lectura.SedeId).OnDelete(DeleteBehavior.Restrict);

        // Para la lista de la caja: las pendientes de la sede, por hora.
        builder.HasIndex(lectura => new { lectura.SedeId, lectura.Estado, lectura.FechaHora });
    }
}
