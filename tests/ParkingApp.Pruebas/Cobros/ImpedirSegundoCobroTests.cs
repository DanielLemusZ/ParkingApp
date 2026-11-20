using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Cobros;

// Escenarios del issue #12 (F-03, RN-03, RN-04 y S-02): un solo cobro por vehículo, sede y día operativo.
// El cobro del día que ya existe está pendiente porque el pago llega con su propia historia;
// RN-04 aplica igual a un cobro pendiente que a uno pagado.
public class ImpedirSegundoCobroTests
{
    [Fact]
    public async Task Carro_con_cobro_del_dia_reingresa_a_las_1500_y_a_las_2200_sin_cobros_nuevos()
    {
        // Miércoles 18 de noviembre, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // 15:00 en Bogotá = 20:00 UTC.
        carril.Reloj.Advance(TimeSpan.FromHours(7));
        var reingresoDeLaTarde = await carril.IngresarAsync("ABC123");

        // 22:00 en Bogotá = jueves 19, 03:00 UTC.
        carril.Reloj.Advance(TimeSpan.FromHours(7));
        var reingresoDeLaNoche = await carril.IngresarAsync("ABC123");

        Assert.True(reingresoDeLaTarde.AbreBarrera);
        Assert.True(reingresoDeLaNoche.AbreBarrera);
        Assert.Null(reingresoDeLaTarde.Ingreso?.Cobro);
        Assert.Null(reingresoDeLaNoche.Ingreso?.Cobro);
        Assert.Equal(3, carril.Eventos.Eventos.Count);
        Assert.Single(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Ingreso_a_la_0100_del_jueves_comparte_el_cobro_del_ingreso_de_las_2300_del_miercoles()
    {
        // Miércoles 18 de noviembre, 23:00 en Bogotá = jueves 19, 04:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 19, 4, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // Jueves 19, 01:00 en Bogotá = 06:00 UTC: sigue siendo el día operativo del miércoles (S-02).
        carril.Reloj.Advance(TimeSpan.FromHours(2));
        var reingreso = await carril.IngresarAsync("ABC123");

        Assert.True(reingreso.AbreBarrera);
        Assert.Null(reingreso.Ingreso?.Cobro);
        var cobro = Assert.Single(carril.Cobros.Cobros);
        Assert.Equal(new DateOnly(2026, 11, 18), cobro.DiaOperativo);
        Assert.Equal(FranjaTarifaria.Nocturna, cobro.Franja);
    }

    [Fact]
    public async Task Carro_con_cobro_del_lunes_que_vuelve_el_martes_a_las_0700_genera_cobro_diurno_nuevo()
    {
        // Lunes 16 de noviembre, 08:00 en Bogotá = 13:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // Martes 17, 07:00 en Bogotá = 12:00 UTC: es otro día operativo.
        carril.Reloj.Advance(TimeSpan.FromHours(23));
        var ingresoDelMartes = await carril.IngresarAsync("ABC123");

        Assert.Equal("Cobro diurno pendiente: $8.000", ingresoDelMartes.Motivo);
        Assert.Equal(2, carril.Cobros.Cobros.Count);
        var cobroDelMartes = carril.Cobros.Cobros[1];
        Assert.Equal(new DateOnly(2026, 11, 17), cobroDelMartes.DiaOperativo);
        Assert.Equal(FranjaTarifaria.Diurna, cobroDelMartes.Franja);
        Assert.Equal(cobroDelMartes.Id, ingresoDelMartes.Ingreso?.Cobro?.Id);
    }

    [Fact]
    public async Task Si_otra_solicitud_guarda_el_cobro_primero_el_ingreso_se_registra_sin_cobro_y_sin_error()
    {
        // Miércoles 18 de noviembre, 08:00 en Bogotá = 13:00 UTC. El primer ingreso guarda el cobro.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // El segundo ingreso, simultáneo, no ve ese cobro al consultar; la restricción única lo rechaza al guardar.
        carril.Cobros.OtraSolicitudGuardoElCobroDespuesDeLaConsulta = true;
        var ingresoSimultaneo = await carril.IngresarAsync("ABC123");

        Assert.True(ingresoSimultaneo.AbreBarrera);
        Assert.Equal(TextosDeCobro.Reingreso(new DateOnly(2026, 11, 18)), ingresoSimultaneo.Motivo);
        Assert.Null(ingresoSimultaneo.Ingreso?.Cobro);
        Assert.Equal(2, carril.Eventos.Eventos.Count);
        Assert.Single(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Ingreso_a_las_0600_del_jueves_genera_cobro_nuevo_aunque_haya_uno_de_las_0559()
    {
        // Jueves 19 de noviembre, 05:59 en Bogotá = 10:59 UTC: día operativo del miércoles.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 19, 10, 59, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // 06:00 en Bogotá = 11:00 UTC: empieza el día operativo del jueves.
        carril.Reloj.Advance(TimeSpan.FromMinutes(1));
        var ingresoDeLas0600 = await carril.IngresarAsync("ABC123");

        Assert.Equal("Cobro diurno pendiente: $8.000", ingresoDeLas0600.Motivo);
        Assert.Equal(2, carril.Cobros.Cobros.Count);
        Assert.Equal(new DateOnly(2026, 11, 18), carril.Cobros.Cobros[0].DiaOperativo);
        Assert.Equal(new DateOnly(2026, 11, 19), carril.Cobros.Cobros[1].DiaOperativo);
    }

    [Fact]
    public async Task El_reingreso_muestra_en_la_barrera_que_ya_existe_el_cobro_del_dia_operativo()
    {
        // Miércoles 18 de noviembre, 08:00 y luego 12:00 en Bogotá.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Reloj.Advance(TimeSpan.FromHours(4));

        var reingreso = await carril.IngresarAsync("ABC123");

        Assert.Equal("Reingreso sin cobro nuevo: ya existe el cobro del día operativo 2026-11-18", reingreso.Motivo);
    }

    [Fact]
    public async Task El_cobro_del_mismo_vehiculo_en_otra_sede_no_cuenta_como_cobro_del_dia()
    {
        // Miércoles 18 de noviembre, 08:00 en Bogotá = 13:00 UTC.
        var ahoraEnUtc = new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);
        var carril = new CarrilDeEntradaDePrueba(ahoraEnUtc);
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
        var cobroDeOtraSede = Cobro.Generar(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, Guid.CreateVersion7(), new DateOnly(2026, 11, 18), tarifa, ahoraEnUtc);
        await carril.Cobros.AgregarAsync(cobroDeOtraSede, TestContext.Current.CancellationToken);

        var ingreso = await carril.IngresarAsync("ABC123");

        Assert.Equal("Cobro diurno pendiente: $8.000", ingreso.Motivo);
        Assert.Equal(2, carril.Cobros.Cobros.Count);
    }
}
