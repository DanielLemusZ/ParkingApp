using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Cobros;

// Criterio 1 del issue #13: la caja lista los cobros pendientes y permite buscar por placa.
public class ListarCobrosPendientesTests
{
    // Miércoles 18 de noviembre de 2026, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
    private static readonly DateTimeOffset MiercolesALas0800 = new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    private static readonly string[] PlacasConABC = ["ABC123", "ABC456"];

    private readonly CarrilDeEntradaDePrueba entrada = new CarrilDeEntradaDePrueba(MiercolesALas0800);

    [Fact]
    public async Task Buscar_ABC_lista_solo_los_cobros_pendientes_de_placas_que_contienen_ABC()
    {
        await entrada.IngresarAsync("ABC123");
        await entrada.IngresarAsync("XYZ789");
        await entrada.IngresarAsync("ABC456");

        var resultado = await ListarAsync(" abc ");

        Assert.Equal(PlacasConABC, resultado.Select(cobro => cobro.Placa));
    }

    [Fact]
    public async Task Sin_busqueda_lista_todos_los_pendientes_con_hora_de_bogota_franja_y_valor()
    {
        await entrada.IngresarAsync("ABC123");
        await entrada.IngresarAsync("XYZ789");

        var resultado = await ListarAsync(null);

        Assert.Equal(2, resultado.Count);
        var primero = resultado[0];
        Assert.Equal("ABC123", primero.Placa);
        Assert.Equal(8, primero.HoraDeIngreso.Hour);
        Assert.Equal(FranjaTarifaria.Diurna, primero.Franja);
        Assert.Equal(8000m, primero.Valor);
    }

    [Fact]
    public async Task Un_cobro_pagado_no_aparece_en_la_caja()
    {
        await entrada.IngresarAsync("ABC123");
        await entrada.IngresarAsync("XYZ789");
        entrada.Cobros.PagarEnEfectivo(entrada.Cobros.Cobros[0]);

        var resultado = await ListarAsync(null);

        Assert.Equal("XYZ789", Assert.Single(resultado).Placa);
    }

    [Fact]
    public async Task El_cobro_de_un_dia_nuevo_muestra_la_hora_real_de_ingreso_y_que_sigue_adentro()
    {
        // Issue #17: entra el martes 17 a las 18:00 en Bogotá (23:00 UTC) y sigue adentro el miércoles a las 06:00.
        var carril = new CarrilDeEntradaDePrueba(new DateTimeOffset(2026, 11, 17, 23, 0, 0, TimeSpan.Zero));
        await carril.IngresarAsync("ABC123");
        carril.Reloj.SetUtcNow(MiercolesALas0800);
        var cobrosDelDiaNuevo = new GenerarCobrosDelDiaNuevo(carril.Eventos, carril.Cobros, carril.Tarifas, carril.Calendario, carril.Sede, carril.Reloj);
        await cobrosDelDiaNuevo.EjecutarAsync(TestContext.Current.CancellationToken);

        var resultado = await new ListarCobrosPendientes(carril.Cobros, carril.Sede).EjecutarAsync(null, TestContext.Current.CancellationToken);

        var diurno = Assert.Single(resultado, cobro => cobro.Motivo == MotivoDeCobro.SigueAdentro);
        Assert.Equal(new DateTime(2026, 11, 17, 18, 0, 0), diurno.HoraDeIngreso.DateTime);
        Assert.Equal(new DateOnly(2026, 11, 18), diurno.DiaOperativo);
        Assert.Equal("Sigue adentro desde 2026-11-17 18:00 (diurna)", TextosDeCobro.Motivo(diurno.Motivo, diurno.Franja, diurno.HoraDeIngreso));
        Assert.Equal(MotivoDeCobro.Ingreso, Assert.Single(resultado, cobro => cobro.DiaOperativo == new DateOnly(2026, 11, 17)).Motivo);
    }

    private Task<IReadOnlyList<CobroPendiente>> ListarAsync(string? placa)
    {
        var caja = new ListarCobrosPendientes(entrada.Cobros, entrada.Sede);
        return caja.EjecutarAsync(placa, TestContext.Current.CancellationToken);
    }
}
