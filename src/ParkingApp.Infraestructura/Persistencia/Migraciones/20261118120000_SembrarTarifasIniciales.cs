using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using ParkingApp.Infraestructura.Persistencia;

#nullable disable

namespace ParkingApp.Infraestructura.Persistencia.Migraciones;

/// <inheritdoc />
[DbContext(typeof(ParkingAppDbContext))]
[Migration("20261118120000_SembrarTarifasIniciales")]
public partial class SembrarTarifasIniciales : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "tarifas",
            columns: new[] { "id", "tipo_de_vehiculo", "franja", "valor", "vigente_desde", "vigente_hasta" },
            values: new object[,]
            {
                { new Guid("019a9000-0000-7000-8000-000000000001"), "Carro", "Diurna", 8000m, new DateOnly(2026, 11, 1), null },
                { new Guid("019a9000-0000-7000-8000-000000000002"), "Carro", "Nocturna", 5000m, new DateOnly(2026, 11, 1), null },
                { new Guid("019a9000-0000-7000-8000-000000000003"), "Moto", "Diurna", 0m, new DateOnly(2026, 11, 1), null },
                { new Guid("019a9000-0000-7000-8000-000000000004"), "Moto", "Nocturna", 0m, new DateOnly(2026, 11, 1), null },
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "tarifas",
            keyColumn: "id",
            keyValues: new object[]
            {
                new Guid("019a9000-0000-7000-8000-000000000001"),
                new Guid("019a9000-0000-7000-8000-000000000002"),
                new Guid("019a9000-0000-7000-8000-000000000003"),
                new Guid("019a9000-0000-7000-8000-000000000004"),
            });
    }
}
