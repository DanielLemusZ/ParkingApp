using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Cobros;

/// <summary>
/// Genera el cobro diurno de cada día operativo nuevo para los vehículos que siguen adentro (S-05, D-28),
/// para que un pago no cubra estadías de varios días. Se puede correr varias veces sin duplicar cobros (RN-04).
/// </summary>
public sealed class GenerarCobrosDelDiaNuevo
{
    private readonly IRepositorioEventosDeAcceso eventos;
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioTarifas tarifas;
    private readonly CalendarioOperativo calendario;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="eventos">Donde se consultan los vehículos que siguen adentro.</param>
    /// <param name="cobros">Donde se consultan y guardan los cobros.</param>
    /// <param name="tarifas">Donde se consulta la tarifa diurna vigente de cada día.</param>
    /// <param name="calendario">Calcula el día operativo de cada instante.</param>
    /// <param name="sede">Sede donde corre el nodo.</param>
    /// <param name="reloj">Da la hora actual.</param>
    public GenerarCobrosDelDiaNuevo(
        IRepositorioEventosDeAcceso eventos,
        IRepositorioCobros cobros,
        IRepositorioTarifas tarifas,
        CalendarioOperativo calendario,
        ISedeActual sede,
        TimeProvider reloj)
    {
        this.eventos = eventos;
        this.cobros = cobros;
        this.tarifas = tarifas;
        this.calendario = calendario;
        this.sede = sede;
        this.reloj = reloj;
    }

    /// <summary>
    /// Genera los cobros que falten: uno diurno por cada día operativo desde el siguiente al ingreso hasta hoy.
    /// Si la aplicación estuvo apagada, también genera los de los días que no corrió.
    /// </summary>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Cuántos cobros se generaron y cuántos días se omitieron por tarifa $0 o sin tarifa vigente.</returns>
    public async Task<ResultadoDeCobrosDelDiaNuevo> EjecutarAsync(CancellationToken cancelacion = default)
    {
        var hoy = (await calendario.UbicarAsync(reloj.GetUtcNow(), cancelacion)).DiaOperativo;
        var adentro = await eventos.ListarAdentroAsync(sede.Id, cancelacion);
        var generados = 0;
        var omitidos = 0;

        foreach (var vehiculo in adentro)
        {
            var diaDelIngreso = (await calendario.UbicarAsync(vehiculo.FechaHoraDeIngreso, cancelacion)).DiaOperativo;

            foreach (var dia in HorarioDeOperacion.DiasOperativosSiguientes(diaDelIngreso, hoy))
            {
                if (await cobros.ExisteDelDiaAsync(vehiculo.Placa, sede.Id, dia, cancelacion))
                {
                    continue;
                }

                var tarifa = await tarifas.ObtenerVigenteAsync(vehiculo.TipoDeVehiculo, FranjaTarifaria.Diurna, dia, cancelacion);

                // D-16: las motos ($0) no generan cobro. Sin tarifa vigente tampoco: se omite sin detener la tarea.
                if (tarifa is null || !tarifa.GeneraCobro)
                {
                    omitidos++;
                    continue;
                }

                // El cobro queda con la hora en que empezó su día (06:00), aunque se genere más tarde, y guarda por qué
                // se cobra y desde cuándo está adentro el vehículo.
                var inicioDelDia = HorarioDeOperacion.InicioEnUtc(dia, sede.ZonaHoraria);
                var cobro = Cobro.GenerarPorPermanencia(
                    vehiculo.Placa,
                    vehiculo.TipoDeVehiculo,
                    sede.Id,
                    dia,
                    tarifa,
                    vehiculo.FechaHoraDeIngreso,
                    inicioDelDia);

                // false: otra corrida guardó primero el cobro del día, y la restricción única evitó el duplicado.
                if (await cobros.AgregarAsync(cobro, cancelacion))
                {
                    generados++;
                }
            }
        }

        return new ResultadoDeCobrosDelDiaNuevo(generados, omitidos);
    }
}

/// <summary>
/// Resultado de una corrida del cobro del día nuevo.
/// </summary>
/// <param name="Generados">Cobros nuevos guardados.</param>
/// <param name="Omitidos">Días sin cobro por tarifa $0 o sin tarifa diurna vigente.</param>
public sealed record ResultadoDeCobrosDelDiaNuevo(int Generados, int Omitidos);
