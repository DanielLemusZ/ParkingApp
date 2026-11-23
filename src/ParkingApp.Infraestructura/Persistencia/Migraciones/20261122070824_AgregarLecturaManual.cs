using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarLecturaManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "validado_por",
                table: "eventos_de_acceso",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lecturas_pendientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    motivo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    placa_leida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sede_id = table.Column<Guid>(type: "uuid", nullable: false),
                    carril = table.Column<int>(type: "integer", nullable: false),
                    confianza = table.Column<int>(type: "integer", nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    placa_validada = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    validada_por = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    fecha_de_validacion = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    resultado = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lecturas_pendientes", x => x.id);
                    table.CheckConstraint("ck_lecturas_pendientes_carril", "carril > 0");
                    table.CheckConstraint("ck_lecturas_pendientes_confianza", "confianza BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_lecturas_pendientes_validacion", "estado = 'Pendiente' OR (placa_validada IS NOT NULL AND validada_por IS NOT NULL AND fecha_de_validacion IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_lecturas_pendientes_sedes_sede_id",
                        column: x => x.sede_id,
                        principalTable: "sedes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lecturas_pendientes_sede_id_estado_fecha_hora",
                table: "lecturas_pendientes",
                columns: new[] { "sede_id", "estado", "fecha_hora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lecturas_pendientes");

            migrationBuilder.DropColumn(
                name: "validado_por",
                table: "eventos_de_acceso");
        }
    }
}
