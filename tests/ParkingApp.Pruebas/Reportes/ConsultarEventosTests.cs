using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Aplicacion.Configuracion;

using Xunit;

namespace ParkingApp.Pruebas.Reportes;

public class ConsultarEventosTests
{
    [Fact]
    public async Task ConsultarAccesosAsync_FechaInicioMayorQueFin_LanzaArgumentException()
    {
        // Arrange
        var repositorio = new RepositorioConsultasFalso();
        var sedeActual = new SedeActualFalsa { ZonaHoraria = TimeSpan.FromHours(-5) };
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
        var sedeActual = new SedeActualFalsa { ZonaHoraria = TimeSpan.FromHours(-5) };
        var servicio = new ConsultarEventos(repositorio, sedeActual);

        var filtros = new FiltrosConsultaDto
        {
            FechaInicio = new DateTime(2026, 11, 19), 
            FechaFin = new DateTime(2026, 11, 19), // El mismo día para probar que el fin toma hasta el final del día
            Placa = "abc-123"
        };

        // Act
        await servicio.ConsultarAccesosAsync(filtros);

        // Assert
        Assert.True(repositorio.FueLlamado);
        
        // 1. Validar que la placa se limpió (se quitó el guion y se puso en mayúsculas)
        Assert.Equal("ABC123", repositorio.PlacaRecibida);

        // 2. Validar inicio: 19/11/2026 00:00:00 -05:00 a UTC -> 19/11/2026 05:00:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 11, 19, 5, 0, 0, TimeSpan.Zero), repositorio.InicioUtcRecibido);

        // 3. Validar fin: Se suma 1 día (20/11/2026 00:00:00 -05:00) a UTC -> 20/11/2026 05:00:00 UTC
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

            // Retornamos una lista vacía porque en esta prueba no nos importan los resultados, sino los parámetros enviados
            return Task.FromResult(new List<EventoDeAcceso>());
        }
    }

    private sealed class SedeActualFalsa : ISedeActual
    {
        public TimeSpan ZonaHoraria { get; set; }

        // Si tu interfaz ISedeActual exige más propiedades (como Id, Nombre, etc.), 
        // puedes agregarlas debajo de esta línea así: public string Nombre { get; set; } = "Sede de Prueba";
    }
}