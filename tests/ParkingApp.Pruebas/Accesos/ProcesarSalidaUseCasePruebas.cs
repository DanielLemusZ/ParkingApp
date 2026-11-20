using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Pruebas.Dobles;

using Xunit;

namespace ParkingApp.Pruebas.Accesos;

public class ProcesarSalidaUseCasePruebas
{
    private sealed class SedeActualDePrueba : ISedeActual
    {
        public Guid Id { get; } = Guid.CreateVersion7();

        public TimeZoneInfo ZonaHoraria { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
    }

    private sealed class FakeSalidaRepository : ISalidaRepository
    {
        public decimal? ValorPendiente { get; set; }

        public bool CobroPagado { get; set; }

        public int UmbralConfianza { get; set; } = 80;

        public bool SalidaRegistrada { get; private set; }

        public Placa? UltimaPlacaRegistrada { get; private set; }

        public TipoDeVehiculo? UltimoTipoRegistrado { get; private set; }

        public Task<decimal?> ObtenerValorCobroPendienteAsync(Placa placa, CancellationToken cancelacion = default) =>
            Task.FromResult(ValorPendiente);

        public Task<bool> TieneCobroPagadoEnDiaOperativoAsync(Placa placa, DateOnly diaOperativo, CancellationToken cancelacion = default) =>
            Task.FromResult(CobroPagado);

        public Task<int> ObtenerUmbralConfianzaAsync(Guid sedeId, CancellationToken cancelacion = default) =>
            Task.FromResult(UmbralConfianza);

        public Task RegistrarSalidaAsync(
            Placa placa,
            TipoDeVehiculo tipoDeVehiculo,
            DateTimeOffset fechaHoraUtc,
            Guid sedeId,
            int carril,
            int confianza,
            CancellationToken cancelacion = default)
        {
            SalidaRegistrada = true;
            UltimaPlacaRegistrada = placa;
            UltimoTipoRegistrado = tipoDeVehiculo;
            return Task.CompletedTask;
        }
    }

    private static (ProcesarSalidaUseCase CasoDeUso, FakeSalidaRepository Repo, SedeActualDePrueba Sede) CrearEscenario()
    {
        var repo = new FakeSalidaRepository();
        var sede = new SedeActualDePrueba();
        var calendario = new CalendarioOperativo(sede, new HorarioDeOperacionDePrueba());
        var reloj = new FakeTimeProvider(new DateTimeOffset(2026, 11, 19, 19, 0, 0, TimeSpan.Zero));
        var casoDeUso = new ProcesarSalidaUseCase(repo, sede, calendario, reloj);
        return (casoDeUso, repo, sede);
    }

    [Fact]
    public async Task EjecutarAsync_ConPlacaVacia_LanzaExcepcion()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();

        var excepcion = await Assert.ThrowsAsync<ArgumentException>(() =>
            casoDeUso.EjecutarAsync("", 90.0, sede.Id, 1, TestContext.Current.CancellationToken));

        Assert.Contains("La placa no puede estar vacía.", excepcion.Message);
        Assert.False(repo.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_BajaConfianza_NoAbreYPideValidacion()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();

        var resultado = await casoDeUso.EjecutarAsync("ABC123", 50.0, sede.Id, 1, TestContext.Current.CancellationToken);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: requiere validación de operador por baja confianza", resultado.Mensaje);
        Assert.False(repo.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_ConCobroPendiente_NoAbreYMuestraDeuda()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();
        repo.ValorPendiente = 8000m;

        var resultado = await casoDeUso.EjecutarAsync("ABC123", 95.0, sede.Id, 1, TestContext.Current.CancellationToken);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", resultado.Mensaje);
        Assert.False(repo.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_MotoSinCobro_AbreBarrera()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();

        var resultado = await casoDeUso.EjecutarAsync("XYZ12D", 98.0, sede.Id, 1, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: sin cobro", resultado.Mensaje);
        Assert.True(repo.SalidaRegistrada);
        Assert.Equal(TipoDeVehiculo.Moto, repo.UltimoTipoRegistrado);
    }

    [Fact]
    public async Task EjecutarAsync_CobroPagado_AbreBarrera()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();
        repo.CobroPagado = true;

        var resultado = await casoDeUso.EjecutarAsync("ABC123", 95.0, sede.Id, 1, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: cobro pagado", resultado.Mensaje);
        Assert.True(repo.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_PlacaManualDiplomaticaPagada_AbreBarreraSinExigirFormatoEstandar()
    {
        var (casoDeUso, repo, sede) = CrearEscenario();
        repo.CobroPagado = true;

        var resultado = await casoDeUso.EjecutarAsync("CD1234", 100.0, sede.Id, 1, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: cobro pagado", resultado.Mensaje);
        Assert.True(repo.SalidaRegistrada);
        Assert.Equal("CD1234", repo.UltimaPlacaRegistrada?.Valor);
        Assert.Equal(TipoDeVehiculo.NoClasificado, repo.UltimoTipoRegistrado);
    }
}
