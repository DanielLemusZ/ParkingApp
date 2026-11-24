using System.Net;

using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.ServiciosExternos.Pasarela;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18: adaptador del sandbox de Wompi, sin salir a internet.
public class AdaptadorWompiTests
{
    private static readonly OpcionesDePasarela Opciones = new OpcionesDePasarela
    {
        Proveedor = ProveedorDePasarela.Wompi,
        LlavePublica = "pub_test_llave",
        SecretoIntegridad = "test_integrity_secreto",
    };

    private static readonly SolicitudDePagoEnPasarela Solicitud = new SolicitudDePagoEnPasarela(
        "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e",
        8000m,
        "Parqueadero ABC123",
        new Uri("https://localhost:7001/caja/pasarela/retorno"));

    [Fact]
    public void La_firma_de_integridad_coincide_con_el_ejemplo_de_la_documentacion_de_Wompi()
    {
        // Ejemplo de la documentación de Wompi (docs.wompi.co, "Widget & Checkout Web", firma de integridad).
        var firma = AdaptadorWompi.Firmar("sk8-438k4-xmxm392-sn2m", 2490000, "prod_integrity_Z5mMke9x0k8gpErbDqwrJXMqsI6SFli6");

        Assert.Equal("37c8407747e595535433ef8f6a811d853cd943046624a0ec04662b17bbf33bf5", firma);
    }

    [Fact]
    public async Task Al_iniciar_arma_el_checkout_con_el_valor_en_centavos_y_la_firma()
    {
        var fabrica = new FabricaDeClientesDePrueba(_ => FabricaDeClientesDePrueba.Json("""{ "data": {} }"""));
        var wompi = new AdaptadorWompi(fabrica, Opciones);

        var inicio = await wompi.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.True(inicio.Disponible);
        var url = inicio.UrlDePago!.ToString();
        Assert.StartsWith("https://checkout.wompi.co/p/?public-key=pub_test_llave&currency=COP&amount-in-cents=800000&", url, StringComparison.Ordinal);
        Assert.Contains($"signature:integrity={AdaptadorWompi.Firmar(Solicitud.Referencia, 800000, "test_integrity_secreto")}", inicio.UrlDePago.Query, StringComparison.Ordinal);
        Assert.EndsWith("merchants/pub_test_llave", Assert.Single(fabrica.Solicitudes).AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sin_internet_al_iniciar_la_pasarela_no_esta_disponible()
    {
        var fabrica = new FabricaDeClientesDePrueba(_ => throw new HttpRequestException("Sin conexión"));
        var wompi = new AdaptadorWompi(fabrica, Opciones);

        var inicio = await wompi.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.False(inicio.Disponible);
        Assert.Null(inicio.UrlDePago);
    }

    [Theory]
    [InlineData("APPROVED", EstadoEnPasarela.Aprobada)]
    [InlineData("DECLINED", EstadoEnPasarela.Rechazada)]
    [InlineData("VOIDED", EstadoEnPasarela.Rechazada)]
    [InlineData("ERROR", EstadoEnPasarela.Rechazada)]
    [InlineData("PENDING", EstadoEnPasarela.EnProceso)]
    public async Task El_estado_de_Wompi_se_traduce_al_de_la_caja(string estadoEnWompi, EstadoEnPasarela esperado)
    {
        var json = $$"""
            { "data": { "id": "12345-1700000000-00001", "status": "{{estadoEnWompi}}",
              "reference": "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e", "amount_in_cents": 800000,
              "status_message": "Fondos insuficientes" } }
            """;
        var wompi = new AdaptadorWompi(new FabricaDeClientesDePrueba(_ => FabricaDeClientesDePrueba.Json(json)), Opciones);

        var consulta = await wompi.ConsultarAsync("12345-1700000000-00001", TestContext.Current.CancellationToken);

        Assert.Equal(esperado, consulta.Estado);
        Assert.Equal("12345-1700000000-00001", consulta.TransaccionId);
        Assert.Equal("ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e", consulta.Referencia);
        Assert.Equal(8000m, consulta.Valor);
    }

    [Fact]
    public async Task Un_error_de_Wompi_al_consultar_es_una_falla_tecnica_y_no_un_rechazo()
    {
        var wompi = new AdaptadorWompi(
            new FabricaDeClientesDePrueba(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            Opciones);

        var consulta = await wompi.ConsultarAsync("12345", TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.NoDisponible, consulta.Estado);
    }
}
