using System.Net;

using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.ServiciosExternos.Pasarela;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18: adaptador del sandbox de PayU Latam, sin salir a internet.
public class AdaptadorPayUTests
{
    private static readonly OpcionesDePayU Opciones = new OpcionesDePayU
    {
        MerchantId = "508029",
        AccountId = "512321",
        ApiKey = "llave-de-prueba",
        ApiLogin = "usuario-de-prueba",
    };

    private static readonly SolicitudDePagoEnPasarela Solicitud = new SolicitudDePagoEnPasarela(
        "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e",
        8000m,
        "Parqueadero ABC123",
        new Uri("http://localhost:5288/caja/pasarela/retorno"));

    private const string PingExitoso = """{ "code": "SUCCESS", "error": null, "result": { "payload": "ping" } }""";

    [Fact]
    public void La_firma_es_el_SHA256_de_la_llave_el_comercio_la_referencia_el_valor_y_la_moneda()
    {
        // Mismo texto del ejemplo de la documentación de PayU ("apiKey~merchantId~referenceCode~amount~currency"),
        // firmado con SHA-256 en lugar de MD5. El valor esperado se calculó aparte, con sha256sum.
        var firma = AdaptadorPayU.Firmar("4Vj8eK4rloUd272L48hsrarnUA", "508029", "TestPayU", "20000");

        Assert.Equal("af3899a22336b79db46006491d15813158826f944599bf3bf601e2327f898022", firma);
    }

    [Fact]
    public async Task Al_iniciar_se_arma_el_formulario_firmado_para_el_WebCheckout_del_sandbox()
    {
        var payu = new AdaptadorPayU(new FabricaDeClientesDePrueba(_ => FabricaDeClientesDePrueba.Json(PingExitoso)), Opciones);

        var inicio = await payu.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        var formulario = payu.Formulario(Solicitud.Referencia);

        Assert.True(inicio.Disponible);
        Assert.Equal($"{AdaptadorPayU.RutaDelFormulario}?referencia={Solicitud.Referencia}", inicio.UrlDePago?.ToString());
        Assert.NotNull(formulario);
        Assert.Equal("https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu/", formulario.Accion.ToString());

        var campos = formulario.Campos.ToDictionary(campo => campo.Key, campo => campo.Value);
        Assert.Equal("8000", campos["amount"]);
        Assert.Equal("COP", campos["currency"]);
        Assert.Equal("1", campos["test"]);
        Assert.Equal("SHA256", campos["algorithmSignature"]);
        Assert.Equal(Solicitud.Referencia, campos["referenceCode"]);
        Assert.Equal(AdaptadorPayU.Firmar("llave-de-prueba", "508029", Solicitud.Referencia, "8000"), campos["signature"]);
        Assert.Equal("http://localhost:5288/caja/pasarela/retorno", campos["responseUrl"]);
    }

    [Fact]
    public async Task Sin_respuesta_de_PayU_al_iniciar_la_pasarela_no_esta_disponible()
    {
        var payu = new AdaptadorPayU(new FabricaDeClientesDePrueba(_ => throw new HttpRequestException("Sin conexión")), Opciones);

        var inicio = await payu.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.False(inicio.Disponible);
        Assert.Null(payu.Formulario(Solicitud.Referencia));
    }

    [Theory]
    [InlineData("APPROVED", EstadoEnPasarela.Aprobada)]
    [InlineData("DECLINED", EstadoEnPasarela.Rechazada)]
    [InlineData("ERROR", EstadoEnPasarela.Rechazada)]
    [InlineData("EXPIRED", EstadoEnPasarela.Rechazada)]
    [InlineData("PENDING", EstadoEnPasarela.EnProceso)]
    public async Task El_estado_de_la_orden_en_PayU_se_traduce_al_de_la_caja(string estadoEnPayU, EstadoEnPasarela esperado)
    {
        var json = $$"""
            { "code": "SUCCESS", "error": null, "result": { "payload": [ {
              "id": 2153371, "referenceCode": "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e",
              "transactions": [ { "id": "5b1e6f3c-0000-4c6a-9d2e-1a2b3c4d5e6f",
                "transactionResponse": { "state": "{{estadoEnPayU}}", "responseCode": "X", "responseMessage": "Fondos insuficientes" },
                "additionalValues": { "TX_VALUE": { "value": 8000.00, "currency": "COP" } } } ] } ] } }
            """;
        var payu = new AdaptadorPayU(new FabricaDeClientesDePrueba(_ => FabricaDeClientesDePrueba.Json(json)), Opciones);

        var consulta = await payu.ConsultarAsync(Solicitud.Referencia, TestContext.Current.CancellationToken);

        Assert.Equal(esperado, consulta.Estado);
        Assert.Equal("5b1e6f3c-0000-4c6a-9d2e-1a2b3c4d5e6f", consulta.TransaccionId);
        Assert.Equal(Solicitud.Referencia, consulta.Referencia);
        Assert.Equal(8000m, consulta.Valor);
        Assert.Equal("Fondos insuficientes", consulta.Motivo);
    }

    [Fact]
    public async Task Sin_orden_todavia_el_pago_sigue_en_proceso()
    {
        var json = """{ "code": "SUCCESS", "error": null, "result": { "payload": null } }""";
        var payu = new AdaptadorPayU(new FabricaDeClientesDePrueba(_ => FabricaDeClientesDePrueba.Json(json)), Opciones);

        var consulta = await payu.ConsultarAsync(Solicitud.Referencia, TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.EnProceso, consulta.Estado);
    }

    [Fact]
    public async Task Un_error_de_PayU_al_consultar_es_una_falla_tecnica_y_no_un_rechazo()
    {
        var payu = new AdaptadorPayU(
            new FabricaDeClientesDePrueba(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)),
            Opciones);

        var consulta = await payu.ConsultarAsync(Solicitud.Referencia, TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.NoDisponible, consulta.Estado);
    }
}
