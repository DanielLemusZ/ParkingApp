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

    [Theory]
    [InlineData(6, 6, 10, 10, 80, Sede.FranjasConMismoInicio)]
    [InlineData(6, 18, -1, 10, 80, Sede.CupoNegativo)]
    [InlineData(6, 18, 10, -1, 80, Sede.CupoNegativo)]
    [InlineData(6, 18, 10, 10, -1, Sede.UmbralConfianzaFueraDeRango)]
    [InlineData(6, 18, 10, 10, 101, Sede.UmbralConfianzaFueraDeRango)]
    public void Configurar_rechaza_valores_invalidos(
        int horaDiurna,
        int horaNocturna,
        int cupoCarros,
        int cupoMotos,
        int umbral,
        string motivoEsperado)
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");

        var motivo = sede.Configurar(
            new TimeOnly(horaDiurna, 0), new TimeOnly(horaNocturna, 0), cupoCarros, cupoMotos, umbral);

        Assert.Equal(motivoEsperado, motivo);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 50)]
    public void Configurar_acepta_umbral_en_los_limites_y_cupo_cero(int umbral, int cupo)
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");

        var motivo = sede.Configurar(new TimeOnly(6, 0), new TimeOnly(18, 0), 0, cupo, umbral);

        Assert.Null(motivo);
        Assert.Equal(0, sede.CupoCarros);
        Assert.Equal(cupo, sede.CupoMotos);
        Assert.Equal(umbral, sede.UmbralConfianza);
    }
}
