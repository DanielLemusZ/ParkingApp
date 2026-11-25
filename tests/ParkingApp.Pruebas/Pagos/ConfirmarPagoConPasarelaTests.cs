using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18: la caja cobra con la pasarela. Bogotá es UTC-5.
public class ConfirmarPagoConPasarelaTests
{
    // Martes 24 de noviembre de 2026, 08:00 en Bogotá = 13:00 UTC: el carro genera el cobro diurno de $8.000.
    private static readonly DateTimeOffset MartesALas0800 = new DateTimeOffset(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba entrada = new CarrilDeEntradaDePrueba(MartesALas0800);
    private readonly RepositorioPagosEnMemoria pagos = new RepositorioPagosEnMemoria();
    private readonly FacturacionDePrueba facturacion = new FacturacionDePrueba();
    private readonly PasarelaDePrueba pasarela = new PasarelaDePrueba();
    private readonly RegistroDePrueba<ConfirmarPagoConPasarela> registro = new RegistroDePrueba<ConfirmarPagoConPasarela>();

    [Fact]
    public async Task Pago_aprobado_por_la_pasarela_deja_el_cobro_pagado_con_su_transaccion()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Aprobada, referencia, 8000m, "12345-1700000000-00001");

        var resultado = await ConfirmarAsync("12345-1700000000-00001");

        Assert.True(resultado.Registrado);
        Assert.Equal("Pago registrado: $8.000", resultado.Motivo);
        Assert.Equal(MedioDePago.Pasarela, resultado.Pago?.Medio);
        Assert.Equal("12345-1700000000-00001", resultado.Pago?.TransaccionDeLaPasarela);
        Assert.Equal(EstadoDeCobro.Pagado, Assert.Single(entrada.Cobros.Cobros).Estado);

        var pago = Assert.Single(pagos.Pagos);
        Assert.Equal(MedioDePago.Pasarela, pago.Medio);
        Assert.Equal("caja", pago.Operador);
        Assert.Equal(MartesALas0800, pago.FechaHora);
        Assert.Single(facturacion.ReferenciasFacturadas);
    }

    [Fact]
    public async Task Cobro_pagado_con_pasarela_da_pase_vigente_el_mismo_dia()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Aprobada, referencia, 8000m);
        await ConfirmarAsync("TX-1");

        // Sale y vuelve a entrar a las 10:00 en Bogotá (15:00 UTC), el mismo día operativo.
        entrada.RegistrarSalida("ABC123", TipoDeVehiculo.Carro);
        entrada.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 24, 15, 0, 0, TimeSpan.Zero));
        var reingreso = await entrada.IngresarAsync("ABC123");

        Assert.True(reingreso.AbreBarrera);
        Assert.Equal(TextosDeCobro.PaseVigente, reingreso.Motivo);
        Assert.Single(entrada.Cobros.Cobros);
    }

    [Fact]
    public async Task Pago_rechazado_por_la_pasarela_deja_el_cobro_pendiente()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Rechazada, referencia, 8000m, motivo: "Fondos insuficientes");

        var resultado = await ConfirmarAsync("TX-1");

        Assert.False(resultado.Registrado);
        Assert.False(resultado.ConsultarDeNuevo);
        Assert.Equal("Pago rechazado por la pasarela: Fondos insuficientes", resultado.Motivo);
        Assert.Equal(EstadoDeCobro.Pendiente, Assert.Single(entrada.Cobros.Cobros).Estado);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task Confirmar_dos_veces_la_misma_transaccion_registra_un_solo_pago()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Aprobada, referencia, 8000m);

        await ConfirmarAsync("TX-1");
        var segundaConfirmacion = await ConfirmarAsync("TX-1");

        Assert.False(segundaConfirmacion.Registrado);
        Assert.Equal("El pago ya estaba registrado", segundaConfirmacion.Motivo);
        Assert.Single(pagos.Pagos);
        Assert.Single(facturacion.ReferenciasFacturadas);
    }

    [Fact]
    public async Task Transaccion_con_valor_distinto_al_pendiente_no_registra_pago()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Aprobada, referencia, 5000m);

        var resultado = await ConfirmarAsync("TX-1");

        Assert.False(resultado.Registrado);
        Assert.Equal(TextosDeCobro.ValorPagadoNoCoincide(5000m, 8000m), resultado.Motivo);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task Pago_en_proceso_o_sin_respuesta_no_cambia_el_cobro_y_se_puede_consultar_de_nuevo()
    {
        var referencia = await CarroConCobroEnviadoALaPasarelaAsync();

        pasarela.Responder(EstadoEnPasarela.EnProceso, referencia, 8000m);
        var enProceso = await ConfirmarAsync("TX-1");
        pasarela.Respuesta = null;
        var sinRespuesta = await ConfirmarAsync("TX-1");

        Assert.True(enProceso.ConsultarDeNuevo);
        Assert.Equal(TextosDeCobro.PagoEnProceso, enProceso.Motivo);
        Assert.True(sinRespuesta.ConsultarDeNuevo);
        Assert.Equal(TextosDeCobro.PasarelaSinRespuesta, sinRespuesta.Motivo);
        Assert.Equal(EstadoDeCobro.Pendiente, Assert.Single(entrada.Cobros.Cobros).Estado);
    }

    [Theory]
    [InlineData("otro-comercio")]
    [InlineData(null)]
    public async Task Transaccion_que_no_inicio_esta_caja_no_registra_pago(string? referenciaAjena)
    {
        await CarroConCobroEnviadoALaPasarelaAsync();
        pasarela.Responder(EstadoEnPasarela.Aprobada, referenciaAjena, 8000m);

        var resultado = await ConfirmarAsync("TX-1");

        Assert.False(resultado.Registrado);
        Assert.Equal(TextosDeCobro.TransaccionDesconocida, resultado.Motivo);
        Assert.Empty(pagos.Pagos);
    }

    // El carro entra a las 08:00 con un cobro diurno de $8.000 y la caja lo manda a la pasarela.
    private async Task<string> CarroConCobroEnviadoALaPasarelaAsync()
    {
        await entrada.IngresarAsync("ABC123");

        var iniciar = new IniciarPagoConPasarela(entrada.Cobros, pagos, pasarela, entrada.Sede);
        var inicio = await iniciar.EjecutarAsync(
            "ABC123",
            8000m,
            Guid.NewGuid(),
            new Uri("https://localhost/caja/pasarela/retorno"),
            TestContext.Current.CancellationToken);

        Assert.True(inicio.Iniciado);
        return pasarela.UltimaSolicitud!.Referencia;
    }

    private Task<ResultadoDeConfirmacionConPasarela> ConfirmarAsync(string transaccionId)
    {
        var confirmar = new ConfirmarPagoConPasarela(entrada.Cobros, pagos, pasarela, facturacion, entrada.Sede, entrada.Reloj, registro);
        return confirmar.EjecutarAsync(transaccionId, "caja", TestContext.Current.CancellationToken);
    }
}
