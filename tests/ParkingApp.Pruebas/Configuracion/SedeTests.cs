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

    [Fact]
    public void Configurar_actualiza_horarios_cupos_y_umbral()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");

        var error = sede.Configurar(new TimeOnly(6, 0), new TimeOnly(19, 0), 60, 40, 85);

        Assert.Null(error);
        Assert.Equal(new TimeOnly(19, 0), sede.InicioFranjaNocturna);
        Assert.Equal(60, sede.CupoCarros);
        Assert.Equal(40, sede.CupoMotos);
        Assert.Equal(85, sede.UmbralConfianza);
    }

    [Fact]
    public void No_permite_horarios_iguales_cupos_negativos_ni_umbral_fuera_de_rango()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");

        Assert.Equal("Las franjas no pueden empezar a la misma hora.", sede.Configurar(new TimeOnly(6, 0), new TimeOnly(6, 0), 1, 1, 80));
        Assert.Equal("El cupo debe ser un número entero mayor o igual a 0.", sede.Configurar(new TimeOnly(6, 0), new TimeOnly(18, 0), -1, 1, 80));
        Assert.Equal("El umbral debe estar entre 0 y 100.", sede.Configurar(new TimeOnly(6, 0), new TimeOnly(18, 0), 1, 1, 120));
    }
}
