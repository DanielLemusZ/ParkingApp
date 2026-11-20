using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Escenarios del issue #13 (F-04 y RN-03): pago en efectivo en la caja, idempotente, que deja pase vigente
// hasta el fin del día operativo.
public class RegistrarPagoEnEfectivoTests
{
    // Miércoles 18 de noviembre de 2026, 08:00 en Bogotá (UTC-5) = 13:00 UTC.
    private static readonly DateTimeOffset MiercolesALas0800 = new DateTimeOffset(2026, 11, 18, 13, 0, 0, TimeSpan.Zero);

    // Lunes 16 de noviembre de 2026, 08:00 en Bogotá = 13:00 UTC.
    private static readonly DateTimeOffset LunesALas0800 = new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba entrada;
    private readonly RepositorioPagosEnMemoria pagos = new RepositorioPagosEnMemoria();
    private readonly FacturacionDePrueba facturacion = new FacturacionDePrueba();

    public RegistrarPagoEnEfectivoTests()
    {
        entrada = new CarrilDeEntradaDePrueba(MiercolesALas0800);
    }

    [Fact]
    public async Task Pagar_en_efectivo_el_cobro_pendiente_de_8000_lo_deja_pagado_con_su_pago()
    {
        await entrada.IngresarAsync("ABC123");
        var cobro = entrada.Cobros.Cobros[0];
        var referencia = Guid.NewGuid();

        // 11:00 en Bogotá = 16:00 UTC.
        entrada.Reloj.Advance(TimeSpan.FromHours(3));
        var resultado = await PagarAsync(cobro.Id, referencia);

        Assert.True(resultado.Registrado);
        Assert.Equal("Pago registrado: $8.000", resultado.Motivo);
        Assert.Equal(EstadoDeCobro.Pagado, cobro.Estado);

        var pago = Assert.Single(pagos.Pagos);
        Assert.Equal(cobro.Id, pago.CobroId);
        Assert.Equal(8000m, pago.Valor);
        Assert.Equal(MedioDePago.Efectivo, pago.Medio);
        Assert.Equal("caja", pago.Operador);
        Assert.Equal(referencia, pago.Referencia);
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 16, 0, 0, TimeSpan.Zero), pago.FechaHora);

        // La caja muestra la hora de Bogotá.
        Assert.Equal(11, resultado.Pago?.FechaHora.Hour);
        Assert.Equal("ABC123", resultado.Pago?.Placa);
    }

    [Fact]
    public async Task El_pago_solicita_la_factura_electronica_con_su_referencia()
    {
        await entrada.IngresarAsync("ABC123");
        var referencia = Guid.NewGuid();

        await PagarAsync(entrada.Cobros.Cobros[0].Id, referencia);

        Assert.Equal(referencia.ToString(), Assert.Single(facturacion.ReferenciasFacturadas));
    }

    [Fact]
    public async Task Carro_que_pago_a_las_1100_reingresa_a_las_1500_sin_cobro_y_con_pase_vigente()
    {
        await entrada.IngresarAsync("ABC123");

        // 11:00 en Bogotá: paga en la caja.
        entrada.Reloj.Advance(TimeSpan.FromHours(3));
        await PagarAsync(entrada.Cobros.Cobros[0].Id, Guid.NewGuid());

        // 15:00 en Bogotá: vuelve a entrar.
        entrada.Reloj.Advance(TimeSpan.FromHours(4));
        var reingreso = await entrada.IngresarAsync("ABC123");

        Assert.True(reingreso.AbreBarrera);
        Assert.Equal("Pase vigente", reingreso.Motivo);
        Assert.Null(reingreso.Ingreso?.Cobro);
        Assert.Single(entrada.Cobros.Cobros);
    }

    [Fact]
    public async Task Pase_del_lunes_no_vale_el_martes_a_las_0700_y_se_genera_cobro_nuevo()
    {
        var lunes = new CarrilDeEntradaDePrueba(LunesALas0800);
        await lunes.IngresarAsync("ABC123");
        var caja = new RegistrarPagoEnEfectivo(lunes.Cobros, pagos, facturacion, lunes.Sede, lunes.Reloj);
        await caja.EjecutarAsync(lunes.Cobros.Cobros[0].Id, Guid.NewGuid(), "caja", TestContext.Current.CancellationToken);

        // Martes 17 a las 07:00 en Bogotá = 12:00 UTC: ya es otro día operativo.
        lunes.Reloj.Advance(TimeSpan.FromHours(23));
        var reingreso = await lunes.IngresarAsync("ABC123");

        Assert.True(reingreso.AbreBarrera);
        Assert.Equal("Cobro diurno pendiente: $8.000", reingreso.Motivo);
        Assert.Equal(2, lunes.Cobros.Cobros.Count);
        Assert.Equal(new DateOnly(2026, 11, 17), reingreso.Ingreso?.Cobro?.DiaOperativo);
    }

    [Fact]
    public async Task Repetir_la_misma_referencia_no_registra_un_segundo_pago()
    {
        await entrada.IngresarAsync("ABC123");
        var cobroId = entrada.Cobros.Cobros[0].Id;
        var referencia = Guid.NewGuid();
        await PagarAsync(cobroId, referencia);

        var repetido = await PagarAsync(cobroId, referencia);

        Assert.False(repetido.Registrado);
        Assert.Equal("El pago ya estaba registrado", repetido.Motivo);
        Assert.Single(pagos.Pagos);
        Assert.Single(facturacion.ReferenciasFacturadas);
    }

    [Fact]
    public async Task Dos_clics_simultaneos_con_la_misma_referencia_registran_un_solo_pago()
    {
        await entrada.IngresarAsync("ABC123");
        var referencia = Guid.NewGuid();

        // La otra solicitud guardó su pago entre la consulta de la referencia y el guardado de esta.
        var otroCobro = Cobro.Generar(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, entrada.Sede.Id, new DateOnly(2026, 11, 18), TarifaDiurna(), MiercolesALas0800);
        pagos.AgregarDeOtraSolicitud(otroCobro.Pagar(MedioDePago.Efectivo, "caja", referencia, MiercolesALas0800));
        pagos.OtraSolicitudGuardoElPagoDespuesDeLaConsulta = true;

        var resultado = await PagarAsync(entrada.Cobros.Cobros[0].Id, referencia);

        Assert.False(resultado.Registrado);
        Assert.Equal("El pago ya estaba registrado", resultado.Motivo);
        Assert.Single(pagos.Pagos);
        Assert.Empty(facturacion.ReferenciasFacturadas);
    }

    [Fact]
    public async Task Pagar_un_cobro_ya_pagado_con_otra_referencia_se_rechaza()
    {
        await entrada.IngresarAsync("ABC123");
        var cobroId = entrada.Cobros.Cobros[0].Id;
        await PagarAsync(cobroId, Guid.NewGuid());

        var resultado = await PagarAsync(cobroId, Guid.NewGuid());

        Assert.False(resultado.Registrado);
        Assert.Equal("El cobro no está pendiente", resultado.Motivo);
        Assert.Single(pagos.Pagos);
    }

    [Fact]
    public async Task Pagar_un_cobro_que_no_existe_se_rechaza()
    {
        var resultado = await PagarAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(resultado.Registrado);
        Assert.Equal("Cobro no encontrado", resultado.Motivo);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task Pagar_el_cobro_de_otra_sede_se_rechaza_como_no_encontrado()
    {
        var cobroDeOtraSede = Cobro.Generar(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, Guid.CreateVersion7(), new DateOnly(2026, 11, 18), TarifaDiurna(), MiercolesALas0800);
        await entrada.Cobros.AgregarAsync(cobroDeOtraSede, TestContext.Current.CancellationToken);

        var resultado = await PagarAsync(cobroDeOtraSede.Id, Guid.NewGuid());

        Assert.Equal("Cobro no encontrado", resultado.Motivo);
        Assert.Equal(EstadoDeCobro.Pendiente, cobroDeOtraSede.Estado);
    }

    private static Tarifa TarifaDiurna()
    {
        return new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m, new DateOnly(2026, 11, 1));
    }

    private Task<ResultadoDePago> PagarAsync(Guid cobroId, Guid referencia)
    {
        var caja = new RegistrarPagoEnEfectivo(entrada.Cobros, pagos, facturacion, entrada.Sede, entrada.Reloj);
        return caja.EjecutarAsync(cobroId, referencia, "caja", TestContext.Current.CancellationToken);
    }
}
