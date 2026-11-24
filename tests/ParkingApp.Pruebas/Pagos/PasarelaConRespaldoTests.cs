using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Pagos;
using ParkingApp.Infraestructura.ServiciosExternos.Pasarela;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Pagos;

// Issue #18: si la pasarela falla tres veces seguidas, la caja cobra solo en efectivo durante cinco minutos.
public class PasarelaConRespaldoTests
{
    private static readonly SolicitudDePagoEnPasarela Solicitud = new SolicitudDePagoEnPasarela(
        "ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e",
        8000m,
        "Parqueadero ABC123",
        new Uri("/caja/pasarela/retorno", UriKind.Relative));

    // Martes 24 de noviembre de 2026, 08:00 en Bogotá = 13:00 UTC.
    private readonly FakeTimeProvider reloj = new FakeTimeProvider(new DateTimeOffset(2026, 11, 24, 13, 0, 0, TimeSpan.Zero));
    private readonly PasarelaDePrueba wompi = new PasarelaDePrueba();
    private readonly AdaptadorPasarelaSimulada simulada = new AdaptadorPasarelaSimulada();
    private readonly RegistroDePrueba<PasarelaConRespaldo> registro = new RegistroDePrueba<PasarelaConRespaldo>();

    [Fact]
    public async Task Tres_fallas_seguidas_dejan_la_pasarela_no_disponible()
    {
        var pasarela = Crear(respaldoSimulado: false);
        wompi.FallaAlIniciar = true;

        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        Assert.True(pasarela.Disponible);

        var tercera = await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.False(tercera.Disponible);
        Assert.False(pasarela.Disponible);
    }

    [Fact]
    public async Task La_pasarela_vuelve_a_intentar_despues_de_cinco_minutos()
    {
        var pasarela = Crear(respaldoSimulado: false);
        wompi.FallaAlIniciar = true;

        for (var intento = 0; intento < 3; intento++)
        {
            await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        }

        // A las 08:04:59 sigue solo en efectivo; a las 08:05 vuelve a intentar con la pasarela, que ya responde.
        reloj.Advance(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        Assert.False(pasarela.Disponible);

        reloj.Advance(TimeSpan.FromSeconds(1));
        wompi.FallaAlIniciar = false;
        var inicio = await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.True(pasarela.Disponible);
        Assert.True(inicio.Disponible);
    }

    [Fact]
    public async Task Un_exito_reinicia_la_cuenta_de_fallas()
    {
        var pasarela = Crear(respaldoSimulado: false);

        wompi.FallaAlIniciar = true;
        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        wompi.FallaAlIniciar = false;
        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);
        wompi.FallaAlIniciar = true;
        await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.True(pasarela.Disponible);
    }

    [Fact]
    public async Task Un_rechazo_de_la_tarjeta_no_cuenta_como_falla()
    {
        var pasarela = Crear(respaldoSimulado: false);
        wompi.Responder(EstadoEnPasarela.Rechazada, Solicitud.Referencia, 8000m, motivo: "Fondos insuficientes");

        for (var intento = 0; intento < 3; intento++)
        {
            await pasarela.ConsultarAsync("TX-1", TestContext.Current.CancellationToken);
        }

        Assert.True(pasarela.Disponible);
    }

    [Fact]
    public async Task Con_respaldo_simulado_la_falla_usa_la_pasarela_simulada()
    {
        var pasarela = Crear(respaldoSimulado: true);
        wompi.FallaAlIniciar = true;

        var inicio = await pasarela.IniciarAsync(Solicitud, TestContext.Current.CancellationToken);

        Assert.True(inicio.Disponible);
        Assert.StartsWith(AdaptadorPasarelaSimulada.RutaDePago, inicio.UrlDePago!.ToString(), StringComparison.Ordinal);

        // La transacción simulada se consulta en la simulada, no en Wompi.
        var id = Uri.UnescapeDataString(inicio.UrlDePago.ToString().Split("transaccion=")[1]);
        simulada.Resolver(id, aprobar: true);
        var consulta = await pasarela.ConsultarAsync(id, TestContext.Current.CancellationToken);

        Assert.Equal(EstadoEnPasarela.Aprobada, consulta.Estado);
        Assert.Equal(Solicitud.Referencia, consulta.Referencia);
    }

    private PasarelaConRespaldo Crear(bool respaldoSimulado)
    {
        var opciones = new OpcionesDePasarela
        {
            Proveedor = ProveedorDePasarela.Wompi,
            RespaldoSimulado = respaldoSimulado,
            FallasAntesDeSoloEfectivo = 3,
            MinutosSinPasarela = 5,
        };

        return new PasarelaConRespaldo(wompi, simulada, opciones, reloj, registro);
    }
}
