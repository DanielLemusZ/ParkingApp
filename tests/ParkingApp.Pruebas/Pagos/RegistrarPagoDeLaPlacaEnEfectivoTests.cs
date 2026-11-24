using Microsoft.Extensions.Logging;

using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Ajuste acordado en el issue #17: la caja paga de una vez todo lo que debe la placa desde que entró, aunque hayan
// pasado varios días. Bogotá es UTC-5: las 06:00 en Bogotá son las 11:00 UTC.
public class RegistrarPagoDeLaPlacaEnEfectivoTests
{
    // Lunes 16 de noviembre de 2026, 18:00 en Bogotá = 23:00 UTC.
    private static readonly DateTimeOffset LunesALas1800 = new DateTimeOffset(2026, 11, 16, 23, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba entrada = new CarrilDeEntradaDePrueba(LunesALas1800);
    private readonly RepositorioPagosEnMemoria pagos = new RepositorioPagosEnMemoria();
    private readonly FacturacionDePrueba facturacion = new FacturacionDePrueba();
    private readonly RegistroDePrueba<RegistrarPagoDeLaPlacaEnEfectivo> registro = new RegistroDePrueba<RegistrarPagoDeLaPlacaEnEfectivo>();

    [Fact]
    public async Task Carro_que_lleva_tres_dias_paga_todo_de_una_vez_y_puede_salir()
    {
        await CarroQueSigueAdentroHastaElMiercolesAsync();

        // Miércoles 18, 10:00 en Bogotá = 15:00 UTC.
        entrada.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 18, 15, 0, 0, TimeSpan.Zero));
        var referencia = Guid.NewGuid();
        var resultado = await PagarAsync(" abc-123 ", referencia);

        Assert.True(resultado.Registrado);
        Assert.Equal("Pago registrado: $21.000 (3 cobros)", resultado.Motivo);
        Assert.Equal(21000m, resultado.Pago?.Total);
        Assert.Equal(3, resultado.Pago?.Cobros);
        Assert.Equal("ABC123", resultado.Pago?.Placa);
        Assert.Equal(10, resultado.Pago?.FechaHora.Hour);
        Assert.All(entrada.Cobros.Cobros, cobro => Assert.Equal(EstadoDeCobro.Pagado, cobro.Estado));

        // Un pago por cobro, cada uno con su valor y una referencia distinta; el primero lleva la de la solicitud.
        Assert.Equal(3, pagos.Pagos.Count);
        Assert.Equal(pagos.Pagos.Count, pagos.Pagos.Select(pago => pago.Referencia).Distinct().Count());
        Assert.Equal(referencia, pagos.Pagos[0].Referencia);
        Assert.All(pagos.Pagos, pago => Assert.Equal(MedioDePago.Efectivo, pago.Medio));

        var salida = await new CarrilDeSalidaDePrueba(entrada).SalirAsync("ABC123");
        Assert.True(salida.AbreBarrera);
    }

    [Fact]
    public async Task Repetir_la_misma_solicitud_no_registra_un_segundo_pago()
    {
        await CarroQueSigueAdentroHastaElMiercolesAsync();
        var referencia = Guid.NewGuid();

        await PagarAsync("ABC123", referencia);
        var reintento = await PagarAsync("ABC123", referencia);

        Assert.False(reintento.Registrado);
        Assert.Equal(TextosDeCobro.PagoYaRegistrado, reintento.Motivo);
        Assert.Equal(3, pagos.Pagos.Count);
        Assert.Single(facturacion.ReferenciasFacturadas);
    }

    [Fact]
    public async Task Una_placa_sin_cobros_pendientes_no_registra_pago()
    {
        await entrada.IngresarAsync("ABC123");
        entrada.Cobros.PagarEnEfectivo(Assert.Single(entrada.Cobros.Cobros));

        var resultado = await PagarAsync("ABC123", Guid.NewGuid());

        Assert.False(resultado.Registrado);
        Assert.Equal("La placa ABC123 no tiene cobros pendientes", resultado.Motivo);
        Assert.Empty(pagos.Pagos);
    }

    [Fact]
    public async Task Solo_se_pagan_los_cobros_de_esa_placa()
    {
        await entrada.IngresarAsync("ABC123");
        await entrada.IngresarAsync("XYZ789");

        var resultado = await PagarAsync("ABC123", Guid.NewGuid());

        Assert.Equal(5000m, resultado.Pago?.Total);
        Assert.Equal("Pago registrado: $5.000", resultado.Motivo);
        var otro = Assert.Single(entrada.Cobros.Cobros, cobro => cobro.Placa.Valor == "XYZ789");
        Assert.Equal(EstadoDeCobro.Pendiente, otro.Estado);
    }

    [Fact]
    public async Task Se_pide_una_sola_factura_por_el_total_con_la_referencia_de_la_solicitud()
    {
        await CarroQueSigueAdentroHastaElMiercolesAsync();
        var referencia = Guid.NewGuid();

        await PagarAsync("ABC123", referencia);

        Assert.Equal(referencia.ToString(), Assert.Single(facturacion.ReferenciasFacturadas));
    }

    [Fact]
    public async Task Si_la_factura_falla_el_pago_queda_registrado_y_la_falla_en_el_registro()
    {
        await CarroQueSigueAdentroHastaElMiercolesAsync();
        facturacion.ProveedorCaido = true;

        var resultado = await PagarAsync("ABC123", Guid.NewGuid());

        Assert.True(resultado.Registrado);
        Assert.Equal(3, pagos.Pagos.Count);
        var falla = Assert.Single(registro.Entradas);
        Assert.Equal(LogLevel.Error, falla.Nivel);
    }

    // El carro entra el lunes a las 18:00 y sigue adentro el martes y el miércoles a las 06:00: tres cobros pendientes
    // (nocturno del lunes $5.000, diurnos del martes y del miércoles $8.000 cada uno).
    private async Task CarroQueSigueAdentroHastaElMiercolesAsync()
    {
        await entrada.IngresarAsync("ABC123");
        entrada.Reloj.SetUtcNow(new DateTimeOffset(2026, 11, 18, 11, 0, 0, TimeSpan.Zero));

        var cobrosDelDiaNuevo = new GenerarCobrosDelDiaNuevo(
            entrada.Eventos,
            entrada.Cobros,
            entrada.Tarifas,
            entrada.Calendario,
            entrada.Sede,
            entrada.Reloj);
        await cobrosDelDiaNuevo.EjecutarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, entrada.Cobros.Cobros.Count);
    }

    private Task<ResultadoDePagoDeLaPlaca> PagarAsync(string placa, Guid referencia)
    {
        var caja = new RegistrarPagoDeLaPlacaEnEfectivo(entrada.Cobros, pagos, facturacion, entrada.Sede, entrada.Reloj, registro);
        return caja.EjecutarAsync(placa, referencia, "caja", TestContext.Current.CancellationToken);
    }
}
