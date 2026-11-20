using ParkingApp.Aplicacion.Reportes;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Reportes;

// Escenarios del issue #22 (F-09): consultas del administrador con filtros por placa, fechas y estado.
// La sede de prueba está en Bogotá (UTC-5).
public class ConsultarEventosTests
{
    private readonly RepositorioConsultasDePrueba repositorio = new RepositorioConsultasDePrueba();
    private readonly SedeActualDePrueba sede = new SedeActualDePrueba();
    private readonly ConsultarEventos consultas;

    public ConsultarEventosTests()
    {
        consultas = new ConsultarEventos(repositorio, sede);
    }

    [Fact]
    public async Task Si_la_fecha_inicial_es_posterior_a_la_final_se_rechaza_la_consulta_de_accesos()
    {
        var filtros = new FiltrosConsultaDto { FechaInicio = new DateTime(2026, 11, 20), FechaFin = new DateTime(2026, 11, 19) };

        var error = await Assert.ThrowsAsync<ArgumentException>(() => consultas.ConsultarAccesosAsync(filtros, TestContext.Current.CancellationToken));

        Assert.Equal("La fecha inicial debe ser anterior a la final", error.Message);
        Assert.False(repositorio.FueConsultado);
    }

    [Fact]
    public async Task Si_la_fecha_inicial_es_posterior_a_la_final_se_rechaza_la_consulta_de_cobros()
    {
        var filtros = new FiltrosConsultaDto { FechaInicio = new DateTime(2026, 11, 20), FechaFin = new DateTime(2026, 11, 19) };

        var error = await Assert.ThrowsAsync<ArgumentException>(() => consultas.ConsultarCobrosAsync(filtros, TestContext.Current.CancellationToken));

        Assert.Equal("La fecha inicial debe ser anterior a la final", error.Message);
        Assert.False(repositorio.FueConsultado);
    }

    [Fact]
    public async Task Un_rango_de_un_solo_dia_incluye_ese_dia_completo_en_la_hora_de_la_sede()
    {
        // Jueves 19 de noviembre, de 00:00 a 23:59:59 en Bogotá = de 05:00 UTC del 19 a 05:00 UTC del 20, sin incluirla.
        var filtros = new FiltrosConsultaDto { FechaInicio = new DateTime(2026, 11, 19), FechaFin = new DateTime(2026, 11, 19) };

        await consultas.ConsultarAccesosAsync(filtros, TestContext.Current.CancellationToken);

        Assert.Equal(new DateTimeOffset(2026, 11, 19, 5, 0, 0, TimeSpan.Zero), repositorio.InicioUtcRecibido);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 5, 0, 0, TimeSpan.Zero), repositorio.FinUtcRecibido);
    }

    [Fact]
    public async Task La_placa_parcial_se_busca_en_mayusculas_y_sin_espacios_ni_guiones()
    {
        var filtros = new FiltrosConsultaDto { Placa = " abc-12 " };

        await consultas.ConsultarAccesosAsync(filtros, TestContext.Current.CancellationToken);

        Assert.Equal("ABC12", repositorio.PlacaRecibida);
    }

    [Fact]
    public async Task Sin_filtros_se_consultan_todos_los_accesos()
    {
        await consultas.ConsultarAccesosAsync(new FiltrosConsultaDto(), TestContext.Current.CancellationToken);

        Assert.True(repositorio.FueConsultado);
        Assert.Null(repositorio.InicioUtcRecibido);
        Assert.Null(repositorio.FinUtcRecibido);
        Assert.Null(repositorio.PlacaRecibida);
    }

    [Fact]
    public async Task La_hora_de_cada_acceso_se_muestra_en_la_hora_de_la_sede()
    {
        // Jueves 19 de noviembre, 04:00 UTC = miércoles 18, 23:00 en Bogotá.
        var ingreso = EventoDeAcceso.RegistrarIngreso(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, sede.Id, 1, 95, new DateTimeOffset(2026, 11, 19, 4, 0, 0, TimeSpan.Zero));
        repositorio.Eventos.Add(ingreso);

        var accesos = await consultas.ConsultarAccesosAsync(new FiltrosConsultaDto(), TestContext.Current.CancellationToken);

        var acceso = Assert.Single(accesos);
        Assert.Equal("ABC123", acceso.Placa);
        Assert.Equal("Carro", acceso.TipoVehiculo);
        Assert.Equal("Ingreso", acceso.TipoEvento);
        Assert.Equal(new DateTime(2026, 11, 18, 23, 0, 0), acceso.FechaHora);
        Assert.Equal("1", acceso.Carril);
        Assert.False(acceso.EsLecturaManual);
    }

    [Fact]
    public async Task Los_cobros_se_filtran_por_dia_operativo_estado_y_placa_normalizada()
    {
        // Cobros pendientes de hoy (jueves 19) para una placa escrita con minúsculas y guion.
        var filtros = new FiltrosConsultaDto
        {
            FechaInicio = new DateTime(2026, 11, 19),
            FechaFin = new DateTime(2026, 11, 19),
            Placa = "abc-123",
            EstadoCobro = EstadoDeCobro.Pendiente,
        };

        await consultas.ConsultarCobrosAsync(filtros, TestContext.Current.CancellationToken);

        Assert.Equal(new DateOnly(2026, 11, 19), repositorio.DiaInicioRecibido);
        Assert.Equal(new DateOnly(2026, 11, 19), repositorio.DiaFinRecibido);
        Assert.Equal("ABC123", repositorio.PlacaRecibida);
        Assert.Equal(EstadoDeCobro.Pendiente, repositorio.EstadoRecibido);
    }

    [Fact]
    public async Task El_cobro_se_muestra_con_su_placa_dia_operativo_franja_valor_y_estado()
    {
        var tarifa = new Tarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, 5000m, new DateOnly(2026, 11, 1));
        var cobro = Cobro.Generar(Placa.Crear("ABC123"), TipoDeVehiculo.Carro, sede.Id, new DateOnly(2026, 11, 18), tarifa, new DateTimeOffset(2026, 11, 19, 4, 0, 0, TimeSpan.Zero));
        repositorio.Cobros.Add(cobro);

        var cobros = await consultas.ConsultarCobrosAsync(new FiltrosConsultaDto(), TestContext.Current.CancellationToken);

        var resultado = Assert.Single(cobros);
        Assert.Equal("ABC123", resultado.Placa);
        Assert.Equal(new DateOnly(2026, 11, 18), resultado.DiaOperativo);
        Assert.Equal(FranjaTarifaria.Nocturna, resultado.Franja);
        Assert.Equal(5000m, resultado.Valor);
        Assert.Equal(EstadoDeCobro.Pendiente, resultado.Estado);
    }
}
