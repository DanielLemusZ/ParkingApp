using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Arma el caso de uso del carril de entrada con la misma cadena que la aplicación y repositorios en memoria.
/// Por defecto carga las tarifas semilla: carro $8.000 diurna y $5.000 nocturna, moto $0 en las dos franjas.
/// </summary>
internal sealed class CarrilDeEntradaDePrueba
{
    private static readonly DateOnly InicioDeLasTarifas = new DateOnly(2026, 11, 1);

    public CarrilDeEntradaDePrueba(DateTimeOffset ahoraEnUtc, bool conTarifasSemilla = true)
    {
        Reloj = new FakeTimeProvider(ahoraEnUtc);
        Calendario = new CalendarioOperativo(Sede, Horario);

        if (conTarifasSemilla)
        {
            AgregarTarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Diurna, 8000m);
            AgregarTarifa(TipoDeVehiculo.Carro, FranjaTarifaria.Nocturna, 5000m);
            AgregarTarifa(TipoDeVehiculo.Moto, FranjaTarifaria.Diurna, 0m);
            AgregarTarifa(TipoDeVehiculo.Moto, FranjaTarifaria.Nocturna, 0m);
        }
    }

    public RepositorioEventosDeAccesoEnMemoria Eventos { get; } = new RepositorioEventosDeAccesoEnMemoria();

    public RepositorioCobrosEnMemoria Cobros { get; } = new RepositorioCobrosEnMemoria();

    public RepositorioTarifasEnMemoria Tarifas { get; } = new RepositorioTarifasEnMemoria();

    public SedeActualDePrueba Sede { get; } = new SedeActualDePrueba();

    public HorarioDeOperacionDePrueba Horario { get; } = new HorarioDeOperacionDePrueba();

    public CalendarioOperativo Calendario { get; }

    public FakeTimeProvider Reloj { get; }

    public void AgregarTarifa(TipoDeVehiculo tipoDeVehiculo, FranjaTarifaria franja, decimal valor)
    {
        Tarifas.AgregarAsync(new Tarifa(tipoDeVehiculo, franja, valor, InicioDeLasTarifas)).GetAwaiter().GetResult();
    }

    public Task<ResultadoDeIngreso> IngresarAsync(string? placa, int confianza = 97, int carril = 1)
    {
        return CrearCasoDeUso().EjecutarAsync(placa, confianza, carril, TestContext.Current.CancellationToken);
    }

    public RegistrarIngreso CrearCasoDeUso()
    {
        // Cada caso de uso lleva su propia cadena, como en cada alcance de la aplicación.
        var validaciones = CadenaDeValidacionesDeIngreso.Crear(
            new LecturaValida(),
            new ClasificacionDeVehiculoValida(),
            new TarifaVigente(Tarifas, Calendario));

        return new RegistrarIngreso(validaciones, Eventos, Tarifas, Cobros, Sede, Calendario, Reloj);
    }
}
