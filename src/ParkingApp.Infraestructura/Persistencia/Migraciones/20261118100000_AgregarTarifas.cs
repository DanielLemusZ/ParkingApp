using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using ParkingApp.Infraestructura.Persistencia;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
[DbContext(typeof(ParkingAppDbContext))]
[Migration("20261118100000_AgregarTarifas")]
public partial class AgregarTarifas : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tarifas",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tipo_de_vehiculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                franja = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                valor = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                vigente_hasta = table.Column<DateOnly>(type: "date", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tarifas", x => x.id);
                table.CheckConstraint("ck_tarifas_valor_no_negativo", "valor >= 0");
                table.CheckConstraint("ck_tarifas_vigencia", "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
            });

        migrationBuilder.CreateIndex(
            name: "ix_tarifas_tipo_de_vehiculo_franja_vigente_desde",
            table: "tarifas",
            columns: new[] { "tipo_de_vehiculo", "franja", "vigente_desde" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tarifas_tipo_de_vehiculo_franja_vigente_hasta",
            table: "tarifas",
            columns: new[] { "tipo_de_vehiculo", "franja", "vigente_hasta" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tarifas");
    }
}
