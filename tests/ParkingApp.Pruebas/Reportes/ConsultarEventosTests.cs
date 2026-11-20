using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;

using Xunit;

namespace ParkingApp.Pruebas.Reportes;

public class ConsultarEventosTests
{
    [Fact]
    public async Task ConsultarAccesosAsync_FechaInicioMayorQueFin_LanzaArgumentException()
    {
        // Arrange
        var repositorio = new RepositorioConsultasFalso();
        var sedeActual = new SedeActualFalsa { ZonaHoraria = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(-5), "Test", "Test") };
        var servicio = new ConsultarEventos(repositorio, sedeActual);

        var filtros = new FiltrosConsultaDto
        {
            FechaInicio = new DateTime(2026, 11, 20),
            FechaFin = new DateTime(2026, 11, 19)
        };

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(() => servicio.ConsultarAccesosAsync(filtros));
        Assert.Equal("La fecha de inicio no puede ser posterior a la fecha de fin.", excepcion.Message);
    }

    [Fact]
    public async Task ConsultarAccesosAsync_ConFiltrosValidos_ConvierteFechasAUtcYNormalizaPlaca()
    {
        // Arrange
        var repositorio = new RepositorioConsultasFalso();

        // Simulamos una sede con zona horaria de -5 horas (Colombia)
        var sedeActual = new SedeActualFalsa { ZonaHoraria = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(-5), "Test", "Test") };
        var servicio = new ConsultarEventos(repositorio, sedeActual);

        var filtros = new FiltrosConsultaDto
        {
            FechaInicio = new DateTime(2026, 11, 19),
            FechaFin = new DateTime(2026, 11, 19),
            Placa = "abc-123"
        };

        // Act
        await servicio.ConsultarAccesosAsync(filtros);

        // Assert
        Assert.True(repositorio.FueLlamado);

        Assert.Equal("ABC123", repositorio.PlacaRecibida);
        Assert.Equal(new DateTimeOffset(2026, 11, 19, 5, 0, 0, TimeSpan.Zero), repositorio.InicioUtcRecibido);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 5, 0, 0, TimeSpan.Zero), repositorio.FinUtcRecibido);
    }

    // --- CLASES DE PRUEBA (FAKES) ---

    private sealed class RepositorioConsultasFalso : IRepositorioConsultas
    {
        public bool FueLlamado { get; private set; }
        public DateTimeOffset? InicioUtcRecibido { get; private set; }
        public DateTimeOffset? FinUtcRecibido { get; private set; }
        public string? PlacaRecibida { get; private set; }

        public Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada)
        {
            FueLlamado = true;
            InicioUtcRecibido = fechaInicioUtc;
            FinUtcRecibido = fechaFinUtc;
            PlacaRecibida = placaNormalizada;
            return Task.FromResult(new List<EventoDeAcceso>());
        }
    }

    private sealed class SedeActualFalsa : ISedeActual
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public TimeZoneInfo ZonaHoraria { get; set; } = TimeZoneInfo.Utc;
    }
}
