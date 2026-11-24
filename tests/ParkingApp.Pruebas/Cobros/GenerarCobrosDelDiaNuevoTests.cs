using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Cobros;

// Escenarios del issue #17 (S-05, D-28): cobro del día nuevo para los vehículos que siguen adentro.
// Bogotá es UTC-5: las 06:00 en Bogotá son las 11:00 UTC. El lunes es el 16 de noviembre de 2026.
public class GenerarCobrosDelDiaNuevoTests
{
    private static readonly DateOnly Lunes = new DateOnly(2026, 11, 16);
    private static readonly DateOnly Martes = new DateOnly(2026, 11, 17);
    private static readonly DateOnly Miercoles = new DateOnly(2026, 11, 18);

    [Fact]
    public async Task Carro_que_paso_la_noche_tiene_cobro_diurno_pendiente_y_no_sale_sin_pagarlo()
    {
        // Lunes 16, 05:50 en Bogotá = 10:50 UTC. Pertenece al día operativo del domingo 15.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 10, 50, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Cobros.PagarEnEfectivo(Assert.Single(carril.Cobros.Cobros));

        // 06:00 en Bogotá.
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 16, 11, 0, 0, TimeSpan.Zero));
        await EjecutarAsync(carril);

        var diurno = Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Lunes);
        Assert.Equal(EstadoDeCobro.Pendiente, diurno.Estado);
        Assert.Equal(FranjaTarifaria.Diurna, diurno.Franja);
        Assert.Equal(8000m, diurno.Valor);

        // 06:10 en Bogotá: la barrera no abre hasta que pague el día nuevo.
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 16, 11, 10, 0, TimeSpan.Zero));
        var salida = await new CarrilDeSalidaDePrueba(carril).SalirAsync("ABC123");

        Assert.False(salida.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", salida.Motivo);
    }

    [Fact]
    public async Task Carro_que_entra_el_lunes_y_sale_el_miercoles_tiene_tres_cobros()
    {
        // Lunes 16, 18:00 en Bogotá = 23:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // La tarea corre el martes y el miércoles a las 06:00 en Bogotá.
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));
        await EjecutarAsync(carril);
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 18, 11, 0, 0, TimeSpan.Zero));
        await EjecutarAsync(carril);

        // Miércoles 18, 10:00 en Bogotá = 15:00 UTC: tiene tres cobros pendientes y no puede salir.
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 18, 15, 0, 0, TimeSpan.Zero));
        var salida = await new CarrilDeSalidaDePrueba(carril).SalirAsync("ABC123");

        Assert.Collection(
            carril.Cobros.Cobros.OrderBy(cobro => cobro.DiaOperativo),
            cobro => Assert.Equal((Lunes, FranjaTarifaria.Nocturna, 5000m), (cobro.DiaOperativo, cobro.Franja, cobro.Valor)),
            cobro => Assert.Equal((Martes, FranjaTarifaria.Diurna, 8000m), (cobro.DiaOperativo, cobro.Franja, cobro.Valor)),
            cobro => Assert.Equal((Miercoles, FranjaTarifaria.Diurna, 8000m), (cobro.DiaOperativo, cobro.Franja, cobro.Valor)));
        Assert.False(salida.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $21.000", salida.Motivo);
    }

    [Fact]
    public async Task Al_arrancar_a_las_0800_genera_los_cobros_que_faltan_del_dia()
    {
        // Entra el lunes 16 a las 18:00 en Bogotá; la aplicación estuvo apagada de 05:00 a 08:00 del martes.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        // Martes 17, 08:00 en Bogotá = 13:00 UTC: primera corrida al arrancar.
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 13, 0, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(1, resultado.Generados);
        var diurno = Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Martes);
        Assert.Equal(EstadoDeCobro.Pendiente, diurno.Estado);

        // Queda con la hora en que empezó el día (06:00 en Bogotá = 11:00 UTC), no con la hora en que se generó.
        Assert.Equal(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero), diurno.FechaHora);

        // Y guarda por qué se cobra y desde cuándo está adentro: el lunes a las 18:00 en Bogotá.
        Assert.Equal(MotivoDeCobro.SigueAdentro, diurno.Motivo);
        Assert.Equal(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero), diurno.FechaHoraDeIngreso);
    }

    [Fact]
    public async Task Si_la_tarea_corre_dos_veces_no_duplica_el_cobro()
    {
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));

        var primera = await EjecutarAsync(carril);
        var segunda = await EjecutarAsync(carril);

        Assert.Equal(1, primera.Generados);
        Assert.Equal(0, segunda.Generados);
        Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Martes);
    }

    [Fact]
    public async Task Moto_que_pasa_la_noche_adentro_no_genera_cobro()
    {
        // Lunes 16, 20:00 en Bogotá = martes 17, 01:00 UTC.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 17, 1, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("XYZ12D");

        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Empty(carril.Cobros.Cobros);
        Assert.Equal(0, resultado.Generados);
        Assert.Equal(1, resultado.Omitidos);
    }

    [Fact]
    public async Task Carro_que_salio_antes_de_las_0600_no_genera_cobro()
    {
        // Entra el lunes 16 a las 20:00 en Bogotá y sale el martes a las 05:30 (10:30 UTC), ya pagado.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 17, 1, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Cobros.PagarEnEfectivo(Assert.Single(carril.Cobros.Cobros));
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 10, 30, 0, TimeSpan.Zero));
        carril.RegistrarSalida("ABC123", TipoDeVehiculo.Carro);

        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(0, resultado.Generados);
        var nocturno = Assert.Single(carril.Cobros.Cobros);
        Assert.Equal(Lunes, nocturno.DiaOperativo);
    }

    [Fact]
    public async Task Con_dos_dias_sin_correr_genera_un_cobro_por_cada_dia()
    {
        // Extra: la aplicación estuvo apagada todo el martes; arranca el miércoles 18 a las 06:00 en Bogotá.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 18, 11, 0, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(2, resultado.Generados);
        Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Martes && cobro.Franja == FranjaTarifaria.Diurna);
        Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Miercoles && cobro.Franja == FranjaTarifaria.Diurna);
    }

    [Fact]
    public async Task Antes_de_las_0600_no_genera_cobro_del_dia_nuevo()
    {
        // Extra: entra el lunes 16 a las 18:00; a las 05:59 del martes (10:59 UTC) sigue el día operativo del lunes.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");

        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 10, 59, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(0, resultado.Generados);
        Assert.Single(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Sin_tarifa_diurna_vigente_omite_el_cobro_sin_fallar()
    {
        // Extra: solo existe la tarifa nocturna del carro.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero), conTarifasSemilla: false);
        carril.AgregarTarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, 5000m);
        await carril.IngresarAsync("ABC123");

        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(0, resultado.Generados);
        Assert.Equal(1, resultado.Omitidos);
        Assert.Single(carril.Cobros.Cobros);
    }

    [Fact]
    public async Task Si_otra_corrida_guardo_el_cobro_primero_no_falla_ni_lo_cuenta()
    {
        // Extra: dos corridas a la vez. La consulta no ve el cobro, pero la restricción única lo rechaza al guardar.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero));
        await EjecutarAsync(carril);

        carril.Cobros.OtraSolicitudGuardoElCobroDespuesDeLaConsulta = true;
        var resultado = await EjecutarAsync(carril);

        Assert.Equal(0, resultado.Generados);
        Assert.Single(carril.Cobros.Cobros, cobro => cobro.DiaOperativo == Martes);
    }

    private static Task<ResultadoDeCobrosDelDiaNuevo> EjecutarAsync(CarrilDeEntradaDePrueba carril)
    {
        var casoDeUso = new GenerarCobrosDelDiaNuevo(
            carril.Eventos,
            carril.Cobros,
            carril.Tarifas,
            carril.Calendario,
            carril.Sede,
            carril.Reloj);

        return casoDeUso.EjecutarAsync(TestContext.Current.CancellationToken);
    }
}
