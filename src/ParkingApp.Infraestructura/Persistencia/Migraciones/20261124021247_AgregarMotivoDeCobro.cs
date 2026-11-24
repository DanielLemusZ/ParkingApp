using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarMotivoDeCobro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "fecha_hora_de_ingreso",
                table: "cobros",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "motivo",
                table: "cobros",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ingreso");

            // Los cobros que ya existían salieron de un ingreso: su hora de ingreso es su propia fecha y hora.
            // Después se quitan los valores por defecto, que solo servían para llenar las filas existentes.
            migrationBuilder.Sql("""
                UPDATE cobros SET fecha_hora_de_ingreso = fecha_hora;
                ALTER TABLE cobros ALTER COLUMN fecha_hora_de_ingreso DROP DEFAULT;
                ALTER TABLE cobros ALTER COLUMN motivo DROP DEFAULT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_hora_de_ingreso",
                table: "cobros");

            migrationBuilder.DropColumn(
                name: "motivo",
                table: "cobros");
        }
    }
}
