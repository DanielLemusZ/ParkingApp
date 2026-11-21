using ParkingApp.Aplicacion.Cobros;
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

    private Task<IReadOnlyList<CobroPendiente>> ListarAsync(string? placa)
    {
        var caja = new ListarCobrosPendientes(entrada.Cobros, entrada.Sede);
        return caja.EjecutarAsync(placa, TestContext.Current.CancellationToken);
    }
}
