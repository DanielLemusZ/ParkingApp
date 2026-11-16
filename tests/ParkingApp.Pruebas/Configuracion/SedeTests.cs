using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Configuracion;

public class SedeTests
{
    [Fact]
    public void Una_sede_nueva_recibe_un_identificador_version_7()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");

        Assert.Equal(7, sede.Id.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Una_sede_sin_nombre_no_se_puede_crear(string nombre)
    {
        var error = Assert.Throws<ArgumentException>(() => new Sede(nombre, "America/Bogota"));

        Assert.Equal("nombre", error.ParamName);
    }

    [Fact]
    public void Una_sede_con_zona_horaria_inexistente_no_se_puede_crear()
    {
        var error = Assert.Throws<ArgumentException>(() => new Sede("Sede principal UAM", "Marte/Olimpo"));

        Assert.Equal("zonaHoraria", error.ParamName);
    }
}
