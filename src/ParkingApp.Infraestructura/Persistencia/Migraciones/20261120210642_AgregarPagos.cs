using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cobro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    medio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operador = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    referencia = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_hora = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos", x => x.id);
                    table.CheckConstraint("ck_pagos_valor_positivo", "valor > 0");
                    table.ForeignKey(
                        name: "fk_pagos_cobros_cobro_id",
                        column: x => x.cobro_id,
                        principalTable: "cobros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_pagos_cobro",
                table: "pagos",
                column: "cobro_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pagos_referencia",
                table: "pagos",
                column: "referencia",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagos");
        }
    }
}
