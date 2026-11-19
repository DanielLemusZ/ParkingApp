using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Configuracion;

public class ConfigurarSedeTests
{
    [Fact]
    public async Task Consultar_devuelve_configuracion_y_ocupacion_actual()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");
        var repositorio = new RepositorioSedesEnMemoria(sede, new OcupacionActual(3, 2));
        var casoDeUso = new ConfigurarSede(repositorio);

        var datos = await casoDeUso.ConsultarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new TimeOnly(6, 0), datos.InicioFranjaDiurna);
        Assert.Equal(new TimeOnly(18, 0), datos.InicioFranjaNocturna);
        Assert.Equal(50, datos.CupoCarros);
        Assert.Equal(50, datos.CupoMotos);
        Assert.Equal(80, datos.UmbralConfianza);
        Assert.Equal(new OcupacionActual(3, 2), datos.Ocupacion);
    }

    [Fact]
    public async Task Guardar_actualiza_y_persiste_la_configuracion()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");
        var repositorio = new RepositorioSedesEnMemoria(sede);
        var casoDeUso = new ConfigurarSede(repositorio);

        var error = await casoDeUso.GuardarAsync(new TimeOnly(7, 0), new TimeOnly(19, 0), 60, 40, 85, TestContext.Current.CancellationToken);

        Assert.Null(error);
        Assert.Equal(1, repositorio.VecesGuardado);
        Assert.Equal(new TimeOnly(7, 0), sede.InicioFranjaDiurna);
        Assert.Equal(new TimeOnly(19, 0), sede.InicioFranjaNocturna);
        Assert.Equal(60, sede.CupoCarros);
        Assert.Equal(40, sede.CupoMotos);
        Assert.Equal(85, sede.UmbralConfianza);
    }

    [Theory]
    [InlineData(6, 6, 10, 10, 80, "Las franjas no pueden empezar a la misma hora.")]
    [InlineData(6, 18, -1, 10, 80, "El cupo debe ser un número entero mayor o igual a 0.")]
    [InlineData(6, 18, 10, -1, 80, "El cupo debe ser un número entero mayor o igual a 0.")]
    [InlineData(6, 18, 10, 10, -1, "El umbral debe estar entre 0 y 100.")]
    [InlineData(6, 18, 10, 10, 101, "El umbral debe estar entre 0 y 100.")]
    public async Task Guardar_rechaza_valores_invalidos_y_no_persiste(
        int horaDiurna,
        int horaNocturna,
        int cupoCarros,
        int cupoMotos,
        int umbral,
        string motivoEsperado)
    {
        var repositorio = new RepositorioSedesEnMemoria(new Sede("Sede principal UAM", "America/Bogota"));
        var casoDeUso = new ConfigurarSede(repositorio);

        var motivo = await casoDeUso.GuardarAsync(
            new TimeOnly(horaDiurna, 0), new TimeOnly(horaNocturna, 0), cupoCarros, cupoMotos, umbral,
            TestContext.Current.CancellationToken);

        Assert.Equal(motivoEsperado, motivo);
        Assert.Equal(0, repositorio.VecesGuardado);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 50)]
    public async Task Guardar_acepta_limites_validos(int umbral, int cupo)
    {
        var repositorio = new RepositorioSedesEnMemoria(new Sede("Sede principal UAM", "America/Bogota"));
        var casoDeUso = new ConfigurarSede(repositorio);

        var motivo = await casoDeUso.GuardarAsync(new TimeOnly(6, 0), new TimeOnly(18, 0), 0, cupo, umbral, TestContext.Current.CancellationToken);

        Assert.Null(motivo);
        Assert.Equal(1, repositorio.VecesGuardado);
    }
}
