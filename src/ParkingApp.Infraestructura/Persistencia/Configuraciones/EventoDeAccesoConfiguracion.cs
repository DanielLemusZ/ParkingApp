using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Mapeo de los ingresos y las salidas a la tabla <c>eventos_de_acceso</c>.
/// </summary>
internal sealed class EventoDeAccesoConfiguracion : IEntityTypeConfiguration<EventoDeAcceso>
{
    public void Configure(EntityTypeBuilder<EventoDeAcceso> builder)
    {
        // La base de datos también protege los rangos, por si un dato llega sin pasar por el dominio.
        builder.ToTable(tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_eventos_de_acceso_confianza",
                $"confianza BETWEEN {EventoDeAcceso.ConfianzaMinima} AND {EventoDeAcceso.ConfianzaMaxima}");
            tabla.HasCheckConstraint("ck_eventos_de_acceso_carril", "carril > 0");
        });

        builder.HasKey(evento => evento.Id);
        builder.Property(evento => evento.Id).ValueGeneratedNever();
        // El tipo se guarda como texto ("Ingreso" o "Salida"), para que se entienda al consultar la tabla.
        builder.Property(evento => evento.Tipo).HasConversion<string>().HasMaxLength(10);
        builder.Property(evento => evento.TipoDeVehiculo).HasConversion<string>().HasMaxLength(20);

        // La placa se guarda como texto: al guardar se usa placa.Valor, y al leer se vuelve a crear la Placa.
        builder.Property(evento => evento.Placa)
            .HasConversion(placa => placa.Valor, valor => Placa.Crear(valor))
            .HasMaxLength(Placa.LargoMaximo);

        // El operador de una lectura manual (F-06). Vacío si la lectura fue automática.
        builder.Property(evento => evento.ValidadoPor).HasMaxLength(EventoDeAcceso.LargoMaximoOperador);

        // Cada evento pertenece a una sede existente, y una sede con eventos no se puede borrar.
        builder.HasOne<Sede>().WithMany().HasForeignKey(evento => evento.SedeId).OnDelete(DeleteBehavior.Restrict);

        // Índice de dos columnas (placa y fecha), para las consultas y para saber si un vehículo está adentro.
        builder.HasIndex(evento => new { evento.Placa, evento.FechaHora });
    }
}
