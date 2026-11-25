using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Configuracion;

// Página Cupos: junta los cupos, la ocupación por conteo y los vehículos que siguen adentro, solo con datos existentes.
public class ConsultarCuposTests
{
    private static readonly DateTimeOffset Manana = new(2026, 11, 25, 13, 0, 0, TimeSpan.Zero);

    private readonly SedeActualDePrueba sedeActual = new();
    private readonly RepositorioEventosDeAccesoEnMemoria eventos = new();

    [Fact]
    public async Task Devuelve_la_ocupacion_de_cada_tipo_frente_a_su_cupo()
    {
        var sede = new Sede("Sede principal UAM", "America/Bogota");
        var casoDeUso = new ConsultarCupos(new RepositorioSedesEnMemoria(sede, new OcupacionActual(8, 2)), eventos, sedeActual);

        var panorama = await casoDeUso.ConsultarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new EstadoDeCupo(50, 8), panorama.Carros);
        Assert.Equal(new EstadoDeCupo(50, 2), panorama.Motos);
    }

    [Fact]
    public async Task Lista_solo_los_vehiculos_que_siguen_adentro_ordenados_por_hora_de_ingreso()
    {
        await IngresarAsync("DKR534", TipoDeVehiculo.Carro, Manana.AddMinutes(30));
        await IngresarAsync("ABC12D", TipoDeVehiculo.Moto, Manana);
        await IngresarAsync("MZN841", TipoDeVehiculo.Carro, Manana.AddMinutes(10));
        await eventos.AgregarAsync(
            EventoDeAcceso.RegistrarSalida(Placa.Crear("MZN841"), TipoDeVehiculo.Carro, sedeActual.Id, 1, 95, Manana.AddHours(1)),
            TestContext.Current.CancellationToken);
        var casoDeUso = new ConsultarCupos(
            new RepositorioSedesEnMemoria(new Sede("Sede principal UAM", "America/Bogota")), eventos, sedeActual);

        var panorama = await casoDeUso.ConsultarAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["ABC12D", "DKR534"], panorama.Adentro.Select(vehiculo => vehiculo.Placa.Valor));
        Assert.Equal(TipoDeVehiculo.Moto, panorama.Adentro[0].TipoDeVehiculo);
    }

    [Fact]
    public async Task Sin_vehiculos_adentro_la_lista_queda_vacia()
    {
        var casoDeUso = new ConsultarCupos(
            new RepositorioSedesEnMemoria(new Sede("Sede principal UAM", "America/Bogota")), eventos, sedeActual);

        var panorama = await casoDeUso.ConsultarAsync(TestContext.Current.CancellationToken);

        Assert.Empty(panorama.Adentro);
        Assert.Equal(50, panorama.Carros.Libres);
    }

    private Task IngresarAsync(string placa, TipoDeVehiculo tipo, DateTimeOffset fechaHora)
    {
        return eventos.AgregarAsync(
            EventoDeAcceso.RegistrarIngreso(Placa.Crear(placa), tipo, sedeActual.Id, 1, 95, fechaHora),
            TestContext.Current.CancellationToken);
    }
}
