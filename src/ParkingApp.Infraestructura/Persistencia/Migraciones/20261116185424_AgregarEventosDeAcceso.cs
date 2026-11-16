using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarEventosDeAcceso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eventos_de_acceso",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sede_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carril = table.Column<int>(type: "integer", nullable: false),
                    confianza = table.Column<int>(type: "integer", nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_de_acceso", x => x.id);
                    table.CheckConstraint("ck_eventos_de_acceso_carril", "carril > 0");
                    table.CheckConstraint("ck_eventos_de_acceso_confianza", "confianza BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "fk_eventos_de_acceso_sedes_sede_id",
                        column: x => x.sede_id,
                        principalTable: "sedes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_de_acceso_placa_fecha_hora",
                table: "eventos_de_acceso",
                columns: new[] { "placa", "fecha_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_eventos_de_acceso_sede_id",
                table: "eventos_de_acceso",
                column: "sede_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_de_acceso");
        }
    }
}
