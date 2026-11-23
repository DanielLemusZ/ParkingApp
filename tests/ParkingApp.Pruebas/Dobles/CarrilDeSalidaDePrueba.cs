using ParkingApp.Aplicacion.Accesos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Arma el caso de uso del carril de salida con los mismos repositorios en memoria, reloj y sede del carril de entrada,
/// para probar recorridos completos: entrar, generar el cobro y salir.
/// </summary>
internal sealed class CarrilDeSalidaDePrueba
{
    private readonly CarrilDeEntradaDePrueba entrada;

    public CarrilDeSalidaDePrueba(CarrilDeEntradaDePrueba entrada)
    {
        this.entrada = entrada;
        Sedes = entrada.Sedes;
    }

    /// <summary>
    /// La misma sede de la entrada, con el umbral de confianza inicial de 80.
    /// </summary>
    public RepositorioSedesEnMemoria Sedes { get; }

    public Task<ResultadoDeSalida> SalirAsync(string? placa, int confianza = 97, int carril = 1)
    {
        return CrearCasoDeUso().EjecutarAsync(placa, confianza, carril, TestContext.Current.CancellationToken);
    }

    public RegistrarSalida CrearCasoDeUso()
    {
        return new RegistrarSalida(entrada.Eventos, entrada.Lecturas, entrada.Cobros, Sedes, entrada.Sede, entrada.Calendario, entrada.Reloj);
    }
}
