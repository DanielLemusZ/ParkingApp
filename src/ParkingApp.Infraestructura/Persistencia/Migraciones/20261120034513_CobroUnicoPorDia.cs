using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class CobroUnicoPorDia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cobros_placa_sede_id_dia_operativo",
                table: "cobros");

            migrationBuilder.CreateIndex(
                name: "ux_cobros_placa_sede_dia",
                table: "cobros",
                columns: new[] { "placa", "sede_id", "dia_operativo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_cobros_placa_sede_dia",
                table: "cobros");

            migrationBuilder.CreateIndex(
                name: "ix_cobros_placa_sede_id_dia_operativo",
                table: "cobros",
                columns: new[] { "placa", "sede_id", "dia_operativo" });
        }
    }
}
