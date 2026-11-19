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
        await adaptador.SolicitarFacturaAsync(15000m, fecha, "Parqueadero día", "PAG-001");

        // Assert
        Assert.Single(logger.Registros);
        var registro = logger.Registros[0];
        Assert.Equal(LogLevel.Information, registro.Nivel);
        Assert.Contains("PAG-001", registro.Mensaje);
        Assert.Contains("15000", registro.Mensaje);
        Assert.Contains("Parqueadero día", registro.Mensaje);
    }

    [Fact]
    public async Task SolicitarFacturaAsync_CuandoElRegistroFalla_NoPropagaExcepcion()
    {
        // Arrange
        var logger = new LoggerDePrueba<AdaptadorFacturacionSimulado> { LanzarErrorEnInfo = true };
        var adaptador = new AdaptadorFacturacionSimulado(logger);

        // Act
        var excepcion = await Record.ExceptionAsync(() =>
            adaptador.SolicitarFacturaAsync(8000m, DateTimeOffset.Now, "Parqueadero hora", "PAG-002"));

        // Assert
        Assert.Null(excepcion);
        Assert.Contains(logger.Registros, r => r.Nivel == LogLevel.Error && r.Mensaje.Contains("PAG-002"));
    }

    private sealed class LoggerDePrueba<T> : ILogger<T>
    {
        public bool LanzarErrorEnInfo { get; set; }
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
            if (LanzarErrorEnInfo && logLevel == LogLevel.Information)
            {
                throw new InvalidOperationException("Falla simulada en el sistema de registro.");
            }

            Registros.Add((logLevel, formatter(state, exception)));
        }
    }
}
