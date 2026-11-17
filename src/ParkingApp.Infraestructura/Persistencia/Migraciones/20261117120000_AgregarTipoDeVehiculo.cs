using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using ParkingApp.Infraestructura.Persistencia;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
[DbContext(typeof(ParkingAppDbContext))]
[Migration("20261117120000_AgregarTipoDeVehiculo")]
public partial class AgregarTipoDeVehiculo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "tipo_de_vehiculo",
            table: "eventos_de_acceso",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "NoClasificado");

        migrationBuilder.Sql("""
            UPDATE eventos_de_acceso
            SET tipo_de_vehiculo = CASE
                WHEN placa ~ '^[A-Z]{3}[0-9]{3}$' THEN 'Carro'
                WHEN placa ~ '^[A-Z]{3}[0-9]{2}[A-Z]$' THEN 'Moto'
                ELSE 'NoClasificado'
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "tipo_de_vehiculo",
            table: "eventos_de_acceso");
    }
}
