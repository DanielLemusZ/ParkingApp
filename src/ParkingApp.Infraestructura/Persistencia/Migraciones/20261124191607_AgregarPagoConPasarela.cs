using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPagoConPasarela : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "transaccion_de_la_pasarela",
                table: "pagos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "transaccion_de_la_pasarela",
                table: "pagos");
        }
    }
}
