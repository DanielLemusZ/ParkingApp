using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso del carril de entrada: valida la lectura de la placa y, si pasa todas las validaciones,
/// registra el ingreso, genera el cobro del día si corresponde y abre la barrera. Lo usan la pantalla del carril y la API.
/// </summary>
public sealed class RegistrarIngreso
{
    private readonly ValidacionDeIngreso validaciones;
    private readonly IRepositorioEventosDeAcceso eventos;
    private readonly IRepositorioTarifas tarifas;
    private readonly IRepositorioCobros cobros;
    private readonly ISedeActual sede;
    private readonly CalendarioOperativo calendario;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="validaciones">Primer eslabón de la cadena de validaciones del carril de entrada.</param>
    /// <param name="eventos">Donde se guardan los ingresos.</param>
    /// <param name="tarifas">Donde se consulta la tarifa vigente.</param>
    /// <param name="cobros">Donde se guardan y consultan los cobros.</param>
    /// <param name="sede">Sede donde corre el carril.</param>
    /// <param name="calendario">Calcula el día operativo y la franja del ingreso.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public RegistrarIngreso(
        ValidacionDeIngreso validaciones,
        IRepositorioEventosDeAcceso eventos,
        IRepositorioTarifas tarifas,
        IRepositorioCobros cobros,
        ISedeActual sede,
        CalendarioOperativo calendario,
        TimeProvider reloj)
    {
        this.validaciones = validaciones;
        this.eventos = eventos;
        this.tarifas = tarifas;
        this.cobros = cobros;
        this.sede = sede;
        this.calendario = calendario;
        this.reloj = reloj;
    }

    /// <summary>
    /// Registra el ingreso de un vehículo a partir de la lectura de su placa.
    /// </summary>
    /// <param name="placa">Texto leído de la placa, sin normalizar.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="carril">Número del carril de entrada.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si la barrera abre, el motivo, el ingreso registrado y el cobro generado.</returns>
    public async Task<ResultadoDeIngreso> EjecutarAsync(
        string? placa,
        int confianza,
        int carril,
        CancellationToken cancelacion = default)
    {
        // 1. Se toma la hora una sola vez y la lectura recorre la cadena de validaciones.
        var ahoraEnUtc = reloj.GetUtcNow();
        var solicitud = new SolicitudDeIngreso(Placa.Normalizar(placa), confianza, carril, ahoraEnUtc);
        var validacion = await validaciones.ValidarAsync(solicitud, cancelacion);

        if (!validacion.Permite)
        {
            return new ResultadoDeIngreso(false, validacion.Motivo, null);
        }

        // 2. Se registra el ingreso.
        var placaNormalizada = Placa.Crear(solicitud.Placa);
        var tipoDeVehiculo = ClasificadorDeVehiculo.Clasificar(placaNormalizada)!.Value;
        var ingreso = EventoDeAcceso.RegistrarIngreso(placaNormalizada, tipoDeVehiculo, sede.Id, carril, confianza, ahoraEnUtc);
        await eventos.AgregarAsync(ingreso, cancelacion);

        // 3. Se genera el cobro del día si la tarifa cobra y es el primer ingreso del día operativo.
        // El ingreso se guarda antes: si el cobro no se pudiera guardar, el paso del vehículo ya quedó registrado.
        var momento = await calendario.UbicarAsync(ahoraEnUtc, cancelacion);
        var (motivo, cobro) = await GenerarCobroAsync(placaNormalizada, tipoDeVehiculo, momento, ahoraEnUtc, cancelacion);

        // 4. Se responde con la hora de la sede, que es la que ve el operador.
        var cobroGenerado = cobro is null
            ? null
            : new CobroGenerado(cobro.Id, cobro.Valor, cobro.Franja, cobro.DiaOperativo, cobro.Estado);
        var registrado = new IngresoRegistrado(
            ingreso.Id,
            ingreso.Placa.Valor,
            momento.HoraLocal,
            ingreso.Carril,
            ingreso.Confianza,
            ingreso.TipoDeVehiculo,
            cobroGenerado);

        return new ResultadoDeIngreso(true, motivo, registrado);
    }

    private async Task<(string Motivo, Cobro? Cobro)> GenerarCobroAsync(
        Placa placa,
        TipoDeVehiculo tipoDeVehiculo,
        MomentoOperativo momento,
        DateTimeOffset ahoraEnUtc,
        CancellationToken cancelacion)
    {
        // El eslabón TarifaVigente ya comprobó que la tarifa existe.
        var tarifa = await tarifas.ObtenerVigenteAsync(tipoDeVehiculo, momento.Franja, momento.DiaOperativo, cancelacion);

        if (tarifa is null || !tarifa.GeneraCobro)
        {
            return (TextosDeCobro.SinCobro(tipoDeVehiculo), null);
        }

        // RN-03 y RN-04: si la placa ya tiene el cobro del día operativo, el reingreso no genera otro.
        // Si ese cobro ya está pagado, el vehículo entra con pase vigente (D-27).
        if (await cobros.ExisteDelDiaAsync(placa, sede.Id, momento.DiaOperativo, cancelacion))
        {
            var pagado = await cobros.ExistePagadoDelDiaAsync(placa, sede.Id, momento.DiaOperativo, cancelacion);
            var motivo = pagado ? TextosDeCobro.PaseVigente : TextosDeCobro.Reingreso(momento.DiaOperativo);
            return (motivo, null);
        }

        // Dos ingresos simultáneos pueden pasar la consulta anterior a la vez. La restricción única de la
        // base de datos deja guardar solo uno; el otro ingreso queda registrado sin cobro y sin error.
        var cobro = Cobro.Generar(placa, tipoDeVehiculo, sede.Id, momento.DiaOperativo, tarifa, ahoraEnUtc);

        if (!await cobros.AgregarAsync(cobro, cancelacion))
        {
            return (TextosDeCobro.Reingreso(momento.DiaOperativo), null);
        }

        return (TextosDeCobro.CobroPendiente(cobro.Franja, cobro.Valor), cobro);
    }
}
