using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18: la caja prepara el pago con la pasarela, o cobra en efectivo si la pasarela no está disponible.
public class IniciarPagoConPasarelaTests
{
    // Martes 24 de noviembre de 2026, 08:00 en Bogotá = 13:00 UTC: el carro genera el cobro diurno de $8.000.
    private static readonly DateTimeOffset MartesALas0800 = new DateTimeOffset(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    private static readonly Uri Retorno = new Uri("https://localhost/caja/pasarela/retorno");

    private readonly CarrilDeEntradaDePrueba entrada = new CarrilDeEntradaDePrueba(MartesALas0800);
    private readonly RepositorioPagosEnMemoria pagos = new RepositorioPagosEnMemoria();
    private readonly PasarelaDePrueba pasarela = new PasarelaDePrueba();

    [Fact]
    public async Task La_caja_manda_a_la_pasarela_el_total_con_la_placa_y_la_referencia()
    {
        await entrada.IngresarAsync("ABC123");
        var referencia = Guid.NewGuid();

        var resultado = await IniciarAsync(" abc-123 ", 8000m, referencia);

        Assert.True(resultado.Iniciado);
        Assert.Equal("Pago de $8.000 enviado a la pasarela", resultado.Motivo);
        Assert.NotNull(resultado.UrlDePago);
        Assert.Equal(ReferenciaDePasarela.Escribir("ABC123", referencia), pasarela.UltimaSolicitud?.Referencia);
        Assert.Equal(8000m, pasarela.UltimaSolicitud?.Valor);
        Assert.Equal(Retorno, pasarela.UltimaSolicitud?.UrlDeRetorno);
    }

    [Fact]
    public async Task Sin_pasarela_disponible_la_caja_ofrece_solo_efectivo()
    {
        await entrada.IngresarAsync("ABC123");
        pasarela.Disponible = false;

        var resultado = await IniciarAsync("ABC123", 8000m, Guid.NewGuid());

        Assert.False(resultado.Iniciado);
        Assert.Equal("Pasarela no disponible: cobre en efectivo", resultado.Motivo);
        Assert.Null(pasarela.UltimaSolicitud);
    }

    [Fact]
    public async Task Si_la_pasarela_no_responde_al_iniciar_la_caja_cobra_en_efectivo()
    {
        await entrada.IngresarAsync("ABC123");
        pasarela.FallaAlIniciar = true;

        var resultado = await IniciarAsync("ABC123", 8000m, Guid.NewGuid());

        Assert.False(resultado.Iniciado);
        Assert.Equal(TextosDeCobro.PasarelaNoDisponible, resultado.Motivo);
        Assert.Null(resultado.UrlDePago);
    }

    [Fact]
    public async Task No_se_manda_a_la_pasarela_un_total_distinto_al_mostrado()
    {
        await entrada.IngresarAsync("ABC123");

        var resultado = await IniciarAsync("ABC123", 5000m, Guid.NewGuid());

        Assert.False(resultado.Iniciado);
        Assert.Equal(TextosDeCobro.TotalCambio(8000m), resultado.Motivo);
        Assert.Null(pasarela.UltimaSolicitud);
    }

    [Fact]
    public void La_referencia_de_la_pasarela_se_lee_igual_que_se_escribio_y_rechaza_las_ajenas()
    {
        var referencia = Guid.NewGuid();

        Assert.True(ReferenciaDePasarela.TryLeer(ReferenciaDePasarela.Escribir("ABC123", referencia), out var placa, out var leida));
        Assert.Equal("ABC123", placa);
        Assert.Equal(referencia, leida);

        Assert.False(ReferenciaDePasarela.TryLeer("ABC123", out _, out _));
        Assert.False(ReferenciaDePasarela.TryLeer($"-{referencia:N}", out _, out _));
        Assert.False(ReferenciaDePasarela.TryLeer("ABC123-no-es-guid", out _, out _));
    }

    private Task<ResultadoDeInicioConPasarela> IniciarAsync(string placa, decimal totalEsperado, Guid referencia)
    {
        var iniciar = new IniciarPagoConPasarela(entrada.Cobros, pagos, pasarela, entrada.Sede);
        return iniciar.EjecutarAsync(placa, totalEsperado, referencia, Retorno, TestContext.Current.CancellationToken);
    }
}
