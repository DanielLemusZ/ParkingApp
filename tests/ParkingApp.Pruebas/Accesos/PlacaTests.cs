using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Pruebas.Accesos;

public class PlacaTests
{
    [Theory]
    [InlineData(" abc-123 ", "ABC123")]
    [InlineData("xyz 12d", "XYZ12D")]
    public void Una_placa_con_minusculas_espacios_y_guiones_se_normaliza(string leida, string esperada)
    {
        var placa = Placa.Crear(leida);

        Assert.Equal(esperada, placa.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" - ")]
    public void Una_placa_vacia_no_se_puede_crear(string leida)
    {
        var error = Assert.Throws<ArgumentException>(() => Placa.Crear(leida));

        Assert.StartsWith("La placa es obligatoria", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Una_placa_de_mas_de_10_caracteres_no_se_puede_crear()
    {
        Assert.Throws<ArgumentException>(() => Placa.Crear("ABC1234567X"));
    }
}
