using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ConfigurarFranjasCuposUmbral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cupo_carros",
                table: "sedes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cupo_motos",
                table: "sedes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "inicio_franja_diurna",
                table: "sedes",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "inicio_franja_nocturna",
                table: "sedes",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "umbral_confianza",
                table: "sedes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "sedes",
                keyColumn: "id",
                keyValue: new Guid("019a8000-0000-7000-8000-000000000001"),
                columns: new[] { "cupo_carros", "cupo_motos", "inicio_franja_diurna", "inicio_franja_nocturna", "umbral_confianza" },
                values: new object[] { 50, 50, new TimeOnly(6, 0, 0), new TimeOnly(18, 0, 0), 80 });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sedes_cupos_no_negativos",
                table: "sedes",
                sql: "cupo_carros >= 0 AND cupo_motos >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sedes_franjas_distintas",
                table: "sedes",
                sql: "inicio_franja_diurna <> inicio_franja_nocturna");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sedes_umbral_confianza",
                table: "sedes",
                sql: "umbral_confianza BETWEEN 0 AND 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sedes_cupos_no_negativos",
                table: "sedes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sedes_franjas_distintas",
                table: "sedes");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sedes_umbral_confianza",
                table: "sedes");

            migrationBuilder.DropColumn(
                name: "cupo_carros",
                table: "sedes");

            migrationBuilder.DropColumn(
                name: "cupo_motos",
                table: "sedes");

            migrationBuilder.DropColumn(
                name: "inicio_franja_diurna",
                table: "sedes");

            migrationBuilder.DropColumn(
                name: "inicio_franja_nocturna",
                table: "sedes");

            migrationBuilder.DropColumn(
                name: "umbral_confianza",
                table: "sedes");
        }
    }
}
