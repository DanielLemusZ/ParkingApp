using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

public class RegistrarIngresoTests
{
    // Lunes 16 de noviembre de 2026 a las 08:00 en Bogotá (UTC-5) son las 13:00 en UTC.
    private static readonly DateTimeOffset AhoraEnUtc = new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero);

    private readonly CarrilDeEntradaDePrueba carril = new CarrilDeEntradaDePrueba(AhoraEnUtc);

    [Fact]
    public async Task Un_ingreso_valido_se_registra_con_su_placa_sede_carril_confianza_y_hora_en_utc()
    {
        var resultado = await carril.IngresarAsync("ABC123", 97, 1);

        Assert.True(resultado.AbreBarrera);

        var ingreso = Assert.Single(carril.Eventos.Eventos);
        Assert.Equal(TipoDeEvento.Ingreso, ingreso.Tipo);
        Assert.Equal(TipoDeVehiculo.Carro, ingreso.TipoDeVehiculo);
        Assert.Equal("ABC123", ingreso.Placa.Valor);
        Assert.Equal(carril.Sede.Id, ingreso.SedeId);
        Assert.Equal(1, ingreso.Carril);
        Assert.Equal(97, ingreso.Confianza);
        Assert.Equal(AhoraEnUtc, ingreso.FechaHora);
        Assert.Equal(TimeSpan.Zero, ingreso.FechaHora.Offset);

        // En el carril se muestra en la hora de la sede.
        Assert.NotNull(resultado.Ingreso);
        Assert.Equal(new DateTime(2026, 11, 16, 8, 0, 0), resultado.Ingreso.FechaHora.DateTime);
        Assert.Equal(TimeSpan.FromHours(-5), resultado.Ingreso.FechaHora.Offset);
        Assert.Equal(TipoDeVehiculo.Carro, resultado.Ingreso.TipoDeVehiculo);
    }

    [Fact]
    public async Task Una_placa_de_moto_se_clasifica_y_guarda_como_moto()
    {
        var resultado = await carril.IngresarAsync("xyz-12d");

        Assert.True(resultado.AbreBarrera);
        var ingreso = Assert.Single(carril.Eventos.Eventos);
        Assert.Equal("XYZ12D", ingreso.Placa.Valor);
        Assert.Equal(TipoDeVehiculo.Moto, ingreso.TipoDeVehiculo);
        Assert.Equal(TipoDeVehiculo.Moto, resultado.Ingreso?.TipoDeVehiculo);
    }

    [Theory]
    [InlineData("ABC1234")]
    [InlineData("123ABC")]
    [InlineData("ABC12")]
    public async Task Una_placa_sin_formato_reconocido_se_rechaza_sin_guardarse(string placa)
    {
        var resultado = await carril.IngresarAsync(placa);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(ClasificacionDeVehiculoValida.FormatoNoReconocido, resultado.Motivo);
        Assert.Null(resultado.Ingreso);
        Assert.Empty(carril.Eventos.Eventos);
    }

    [Fact]
    public async Task La_placa_se_registra_normalizada()
    {
        var resultado = await carril.IngresarAsync(" abc-123 ");

        Assert.True(resultado.AbreBarrera);
        Assert.Equal("ABC123", Assert.Single(carril.Eventos.Eventos).Placa.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Una_placa_vacia_se_rechaza_sin_registrar_el_ingreso(string? placa)
    {
        var resultado = await carril.IngresarAsync(placa);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(LecturaValida.PlacaObligatoria, resultado.Motivo);
        Assert.Null(resultado.Ingreso);
        Assert.Empty(carril.Eventos.Eventos);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(120)]
    public async Task Una_confianza_fuera_de_rango_se_rechaza_sin_registrar_el_ingreso(int confianza)
    {
        var resultado = await carril.IngresarAsync("ABC123", confianza);

        Assert.False(resultado.AbreBarrera);
        Assert.Equal(LecturaValida.ConfianzaFueraDeRango, resultado.Motivo);
        Assert.Empty(carril.Eventos.Eventos);
    }
}
