using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarCobros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cobros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo_de_vehiculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sede_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_operativo = table.Column<DateOnly>(type: "date", nullable: false),
                    franja = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tarifa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cobros", x => x.id);
                    table.CheckConstraint("ck_cobros_valor_positivo", "valor > 0");
                    table.ForeignKey(
                        name: "fk_cobros_sedes_sede_id",
                        column: x => x.sede_id,
                        principalTable: "sedes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cobros_tarifas_tarifa_id",
                        column: x => x.tarifa_id,
                        principalTable: "tarifas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cobros_placa_sede_id_dia_operativo",
                table: "cobros",
                columns: new[] { "placa", "sede_id", "dia_operativo" });

            migrationBuilder.CreateIndex(
                name: "ix_cobros_sede_id",
                table: "cobros",
                column: "sede_id");

            migrationBuilder.CreateIndex(
                name: "ix_cobros_tarifa_id",
                table: "cobros",
                column: "tarifa_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cobros");
        }
    }
}
