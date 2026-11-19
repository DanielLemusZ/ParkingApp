using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Tarifas;

public class AdministrarTarifasTests
{
    private static readonly DateOnly FechaInicial = new DateOnly(2026, 11, 18);

    private readonly RepositorioTarifasEnMemoria repositorio = new RepositorioTarifasEnMemoria();
    private readonly FakeTimeProvider reloj = new FakeTimeProvider(
        new DateTimeOffset(2026, 11, 18, 2, 0, 0, TimeSpan.Zero));

    private AdministrarTarifas CasoDeUso => new AdministrarTarifas(repositorio, new SedeActualDePrueba(), reloj);

    [Fact]
    public async Task Cambiar_tarifa_crea_una_version_y_conserva_la_anterior()
    {
        await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000, FechaInicial, TestContext.Current.CancellationToken);
        var cambio = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 9000, FechaInicial.AddDays(1), TestContext.Current.CancellationToken);

        Assert.True(cambio.Guardado);
        Assert.Equal(2, repositorio.Tarifas.Count);
        Assert.Equal(8000, Assert.Single(repositorio.Tarifas, tarifa => tarifa.EstaVigenteEn(FechaInicial)).Valor);
        Assert.Equal(9000, Assert.Single(repositorio.Tarifas, tarifa => tarifa.EstaVigenteEn(FechaInicial.AddDays(1))).Valor);
        Assert.Equal(FechaInicial, repositorio.Tarifas[0].VigenteHasta);
    }

    [Fact]
    public async Task Cambiar_tarifa_con_la_misma_fecha_actualiza_la_version_existente()
    {
        await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 1000, FechaInicial, TestContext.Current.CancellationToken);

        var cambio = await CasoDeUso.CambiarAsync(
            TipoDeVehiculo.Carro,
            FranjaTarifaria.Diurna,
            1200,
            FechaInicial,
            TestContext.Current.CancellationToken);

        Assert.True(cambio.Guardado);
        var tarifaUnica = Assert.Single(repositorio.Tarifas);
        Assert.Equal(1200, tarifaUnica.Valor);
        Assert.Null(tarifaUnica.VigenteHasta);
    }

    [Theory]
    [InlineData(9000)]
    [InlineData(8000)]
    public async Task Tarifa_nocturna_mayor_o_igual_a_diurna_se_rechaza(decimal nocturna)
    {
        await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000, FechaInicial, TestContext.Current.CancellationToken);
        var cambio = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, nocturna, FechaInicial, TestContext.Current.CancellationToken);

        Assert.False(cambio.Guardado);
        Assert.Equal(Tarifa.NocturnaMayorQueDiurna, cambio.Motivo);
        Assert.Single(repositorio.Tarifas);
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(5000)]
    public async Task Tarifa_diurna_menor_o_igual_a_la_nocturna_se_rechaza(decimal diurna)
    {
        await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, 5000, FechaInicial, TestContext.Current.CancellationToken);
        var cambio = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, diurna, FechaInicial, TestContext.Current.CancellationToken);

        Assert.False(cambio.Guardado);
        Assert.Equal(Tarifa.NocturnaMayorQueDiurna, cambio.Motivo);
        Assert.Single(repositorio.Tarifas);
    }

    [Fact]
    public async Task Las_dos_tarifas_en_cero_se_aceptan()
    {
        var diurna = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Moto, FranjaTarifaria.Diurna, 0, FechaInicial, TestContext.Current.CancellationToken);
        var nocturna = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Moto, FranjaTarifaria.Nocturna, 0, FechaInicial, TestContext.Current.CancellationToken);

        Assert.True(diurna.Guardado);
        Assert.True(nocturna.Guardado);
        Assert.Equal(2, repositorio.Tarifas.Count);
    }

    [Fact]
    public async Task Un_valor_negativo_se_rechaza_sin_guardar()
    {
        var cambio = await CasoDeUso.CambiarAsync(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, -1, FechaInicial, TestContext.Current.CancellationToken);

        Assert.False(cambio.Guardado);
        Assert.Equal(Tarifa.ValorNegativo, cambio.Motivo);
        Assert.Empty(repositorio.Tarifas);
    }

    [Fact]
    public void La_fecha_actual_se_calcula_en_la_zona_horaria_de_la_sede()
    {
        Assert.Equal(new DateOnly(2026, 11, 17), CasoDeUso.FechaActualSede());
    }
}
