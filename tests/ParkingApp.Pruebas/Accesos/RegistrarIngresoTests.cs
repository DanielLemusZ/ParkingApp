using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

public class RegistrarIngresoTests
{
    // Lunes 16 de noviembre de 2026 a las 08:00 en Bogotá (UTC-5) son las 13:00 en UTC.
    private static readonly DateTimeOffset AhoraEnUtc = new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

    private readonly RepositorioEventosDeAccesoEnMemoria repositorio = new RepositorioEventosDeAccesoEnMemoria();
    private readonly SedeActualDePrueba sede = new SedeActualDePrueba();
    private readonly FakeTimeProvider reloj = new FakeTimeProvider(AhoraEnUtc);

    [Fact]
    public async Task Un_ingreso_valido_se_registra_con_su_placa_sede_carril_confianza_y_hora_en_utc()
    {
        var resultado = await CrearCasoDeUso().EjecutarAsync("ABC123", 97, 1, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal(RegistrarIngreso.IngresoAceptado, resultado.Motivo);

        var ingreso = Assert.Single(repositorio.Eventos);
        Assert.Equal(TipoDeEvento.Ingreso, ingreso.Tipo);
        Assert.Equal("ABC123", ingreso.Placa.Valor);
        Assert.Equal(sede.Id, ingreso.SedeId);
        Assert.Equal(1, ingreso.Carril);
        Assert.Equal(97, ingreso.Confianza);
        Assert.Equal(AhoraEnUtc, ingreso.FechaHora);
        Assert.Equal(TimeSpan.Zero, ingreso.FechaHora.Offset);

        // En el carril se muestra en la hora de la sede.
        Assert.NotNull(resultado.Ingreso);
        Assert.Equal(new DateTime(2026, 11, 16, 8, 0, 0), resultado.Ingreso.FechaHora.DateTime);
        Assert.Equal(TimeSpan.FromHours(-5), resultado.Ingreso.FechaHora.Offset);
    }

    [Fact]
    public async Task La_placa_se_registra_normalizada()
    {
        var resultado = await CrearCasoDeUso().EjecutarAsync(" abc-123 ", 97, 1, TestContext.Current.CancellationToken);

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("ABC123", Assert.Single(repositorio.Eventos).Placa.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Una_placa_vacia_se_rechaza_sin_registrar_el_ingreso(string? placa)
    {
        var resultado = await CrearCasoDeUso().EjecutarAsync(placa, 97, 1, TestContext.Current.CancellationToken);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(LecturaValida.PlacaObligatoria, resultado.Motivo);
        Assert.Null(resultado.Ingreso);
        Assert.Empty(repositorio.Eventos);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(120)]
    public async Task Una_confianza_fuera_de_rango_se_rechaza_sin_registrar_el_ingreso(int confianza)
    {
        var resultado = await CrearCasoDeUso().EjecutarAsync("ABC123", confianza, 1, TestContext.Current.CancellationToken);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(LecturaValida.ConfianzaFueraDeRango, resultado.Motivo);
        Assert.Empty(repositorio.Eventos);
    }

    private RegistrarIngreso CrearCasoDeUso()
    {
        var validaciones = CadenaDeValidacionesDeIngreso.Crear(new LecturaValida());
        return new RegistrarIngreso(validaciones, repositorio, sede, reloj);
    }
}
