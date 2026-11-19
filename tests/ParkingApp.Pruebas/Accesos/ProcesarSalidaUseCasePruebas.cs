using ParkingApp.Aplicacion.Accesos;

using Xunit;

namespace ParkingApp.Pruebas.Accesos;

public class ProcesarSalidaUseCasePruebas
{
    // Clase falsa (Fake) para simular la base de datos en las pruebas
    private sealed class FakeSalidaRepository : ISalidaRepository
    {
        public decimal? ValorPendiente { get; set; }
        public bool CobroPagado { get; set; }
        public bool SalidaRegistrada { get; private set; }

        public Task<decimal?> ObtenerValorCobroPendienteAsync(string placa) => Task.FromResult(ValorPendiente);

        public Task<bool> TieneCobroPagadoHoyAsync(string placa) => Task.FromResult(CobroPagado);

        public Task RegistrarSalidaAsync(string placa, DateTimeOffset fechaHora, Guid sedeId, int carril, int confianza)
        {
            SalidaRegistrada = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task EjecutarAsync_ConPlacaVacia_LanzaExcepcion()
    {
        // Arrange
        var repoFalso = new FakeSalidaRepository();
        var casoDeUso = new ProcesarSalidaUseCase(repoFalso);

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(() =>
            casoDeUso.EjecutarAsync("", 90.0, Guid.NewGuid(), 1));

        Assert.Equal("La placa no puede estar vacía.", excepcion.Message);
        Assert.False(repoFalso.SalidaRegistrada); // Verifica que no se guardó nada
    }

    [Fact]
    public async Task EjecutarAsync_BajaConfianza_NoAbreYPideValidacion()
    {
        // Arrange
        var repoFalso = new FakeSalidaRepository();
        var casoDeUso = new ProcesarSalidaUseCase(repoFalso);

        // Act
        var resultado = await casoDeUso.EjecutarAsync("ABC123", 50.0, Guid.NewGuid(), 1);

        // Assert
        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: requiere validación de operador por baja confianza", resultado.Mensaje);
        Assert.False(repoFalso.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_ConCobroPendiente_NoAbreYMuestraDeuda()
    {
        // Arrange
        var repoFalso = new FakeSalidaRepository { ValorPendiente = 8000 };
        var casoDeUso = new ProcesarSalidaUseCase(repoFalso);

        // Act
        var resultado = await casoDeUso.EjecutarAsync("ABC123", 95.0, Guid.NewGuid(), 1);

        // Assert
        Assert.False(resultado.AbreBarrera);
        Assert.Equal("No abre: cobro pendiente de $8.000", resultado.Mensaje);
        Assert.False(repoFalso.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_SaleSinCobro_AbreBarrera()
    {
        // Arrange: Moto XYZ12D que no genera cobro
        var repoFalso = new FakeSalidaRepository { ValorPendiente = null, CobroPagado = false };
        var casoDeUso = new ProcesarSalidaUseCase(repoFalso);

        // Act
        var resultado = await casoDeUso.EjecutarAsync("XYZ12D", 98.0, Guid.NewGuid(), 1);

        // Assert
        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: sin cobro", resultado.Mensaje);
        Assert.True(repoFalso.SalidaRegistrada);
    }

    [Fact]
    public async Task EjecutarAsync_CobroPagado_AbreBarrera()
    {
        // Arrange: Carro con cobro ya pagado
        var repoFalso = new FakeSalidaRepository { ValorPendiente = null, CobroPagado = true };
        var casoDeUso = new ProcesarSalidaUseCase(repoFalso);

        // Act
        var resultado = await casoDeUso.EjecutarAsync("ABC123", 95.0, Guid.NewGuid(), 1);

        // Assert
        Assert.True(resultado.AbreBarrera);
        Assert.Equal("Abre: cobro pagado", resultado.Mensaje);
        Assert.True(repoFalso.SalidaRegistrada);
    }
}
