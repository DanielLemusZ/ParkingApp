using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18, criterios 2 y 3: la pasarela simulada no usa internet y deja elegir si el pago se aprueba o se rechaza.
public class AdaptadorPasarelaSimuladaTests
{
    private static readonly SolicitudDePagoEnPasarela Solicitud = new SolicitudDePagoEnPasarela(
        "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e",
        8000m,
        "Parqueadero ABC123",
        new Uri("/caja/pasarela/retorno", UriKind.Relative));

    private readonly AdaptadorPasarelaSimulada simulada = new AdaptadorPasarelaSimulada();

    [Fact]
    public async Task El_operador_aprueba_el_pago_y_la_pasarela_devuelve_la_transaccion_con_su_referencia()
    {
        var id = await IniciarAsync();

        Assert.Equal(8000m, simulada.BuscarPendiente(id)?.Valor);
        var retorno = simulada.Resolver(id, aprobar: true);
        var consulta = await simulada.ConsultarAsync(id, TestContext.Current.CancellationToken);

        Assert.Equal($"/caja/pasarela/retorno?id={id}", retorno?.ToString());
        Assert.Equal(EstadoEnPasarela.Aprobada, consulta.Estado);
        Assert.Equal(Solicitud.Referencia, consulta.Referencia);
        Assert.Equal(8000m, consulta.Valor);
        Assert.Null(simulada.BuscarPendiente(id));
    }

    [Fact]
    public async Task El_operador_rechaza_el_pago_con_un_motivo()
    {
        var id = await IniciarAsync();

        simulada.Resolver(id, aprobar: false, "Fondos insuficientes");
        var consulta = await simulada.ConsultarAsync(id, TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.Rechazada, consulta.Estado);
        Assert.Equal("Fondos insuficientes", consulta.Motivo);
    }

    [Fact]
    public async Task Antes_de_la_decision_el_pago_sigue_en_proceso_y_una_transaccion_ajena_se_rechaza()
    {
        var id = await IniciarAsync();

        var enProceso = await simulada.ConsultarAsync(id, TestContext.Current.CancellationToken);
        var ajena = await simulada.ConsultarAsync("SIM-no-existe", TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.EnProceso, enProceso.Estado);
        Assert.Equal(EstadoEnPasarela.Rechazada, ajena.Estado);
        Assert.Null(simulada.Resolver("SIM-no-existe", aprobar: true));
    }

    private async Task<string> IniciarAsync()
    {
        var inicio = await simulada.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.True(inicio.Disponible);
        var id = IdDe(inicio);
        Assert.StartsWith(AdaptadorPasarelaSimulada.Prefijo, id, StringComparison.Ordinal);
        return id;
    }

    private static string IdDe(InicioDePagoEnPasarela inicio)
    {
        return Uri.UnescapeDataString(inicio.UrlDePago!.ToString().Split("transaccion=")[1]);
    }
}
