using Microsoft.Extensions.Logging;
using ParkingApp.Infraestructura.ServiciosExternos;
using Xunit;

namespace ParkingApp.Pruebas.Pagos;

public class AdaptadorFacturacionSimuladoTests
{
    [Fact]
    public async Task SolicitarFacturaAsync_CuandoDatosSonValidos_RegistraInformacionEnLog()
    {
        // Arrange
        var logger = new LoggerDePrueba<AdaptadorFacturacionSimulado>();
        var adaptador = new AdaptadorFacturacionSimulado(logger);
        var fecha = new DateTimeOffset(2026, 11, 19, 10, 30, 0, TimeSpan.FromHours(-5));

        // Act
        await adaptador.SolicitarFacturaAsync(15000m, fecha, "Parqueadero día", "PAG-001", CancellationToken.None);

        // Assert
        Assert.Single(logger.Registros);
        var registro = logger.Registros[0];
        Assert.Equal(LogLevel.Information, registro.Nivel);
        Assert.Contains("PAG-001", registro.Mensaje);
        Assert.Contains("15000", registro.Mensaje);
        Assert.Contains("Parqueadero día", registro.Mensaje);
    }

    private sealed class LoggerDePrueba<T> : ILogger<T>
    {
        public List<(LogLevel Nivel, string Mensaje)> Registros { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Registros.Add((logLevel, formatter(state, exception)));
        }
    }
}