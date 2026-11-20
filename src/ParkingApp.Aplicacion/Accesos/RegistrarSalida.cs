using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso del carril de salida (F-05): la barrera abre solo si el vehículo no tiene cobros pendientes (RN-06)
/// y, si abre, registra la salida. Lo usan la pantalla del carril y la API.
/// </summary>
public sealed class RegistrarSalida
{
    /// <summary>
    /// Motivo cuando el vehículo tiene pagado el cobro del día operativo.
    /// </summary>
    public const string AbreConCobroPagado = "Abre: cobro pagado";

    /// <summary>
    /// Motivo cuando el vehículo no tiene cobro en el día operativo.
    /// </summary>
    public const string AbreSinCobro = "Abre: sin cobro";

    /// <summary>
    /// Motivo cuando la confianza de la lectura es menor que el umbral de la sede: el operador valida la placa en la caja (F-06).
    /// </summary>
    public const string ConfianzaBaja = "No abre: confianza baja, la placa se valida en la caja";

    private readonly IRepositorioEventosDeAcceso eventos;
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioSedes sedes;
    private readonly ISedeActual sede;
    private readonly CalendarioOperativo calendario;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="eventos">Donde se guardan las salidas.</param>
    /// <param name="cobros">Donde se consultan los cobros del vehículo.</param>
    /// <param name="sedes">Donde se consulta el umbral de confianza de la sede.</param>
    /// <param name="sede">Sede donde corre el carril.</param>
    /// <param name="calendario">Calcula el día operativo de la salida.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public RegistrarSalida(
        IRepositorioEventosDeAcceso eventos,
        IRepositorioCobros cobros,
        IRepositorioSedes sedes,
        ISedeActual sede,
        CalendarioOperativo calendario,
        TimeProvider reloj)
    {
        this.eventos = eventos;
        this.cobros = cobros;
        this.sedes = sedes;
        this.sede = sede;
        this.calendario = calendario;
        this.reloj = reloj;
    }

    /// <summary>
    /// Motivo cuando el vehículo debe cobros, por ejemplo <c>No abre: cobro pendiente de $8.000</c>.
    /// </summary>
    /// <param name="total">Total de los cobros pendientes.</param>
    /// <returns>El motivo que se muestra en el carril.</returns>
    public static string NoAbrePorCobroPendiente(decimal total)
    {
        return $"No abre: cobro pendiente de {TextosDeCobro.Pesos(total)}";
    }

    /// <summary>
    /// Registra la salida de un vehículo a partir de la lectura de su placa.
    /// </summary>
    /// <param name="placa">Texto leído de la placa, sin normalizar.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="carril">Número del carril de salida.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si la barrera abre, el motivo y la salida registrada.</returns>
    public async Task<ResultadoDeSalida> EjecutarAsync(
        string? placa,
        int confianza,
        int carril,
        CancellationToken cancelacion = default)
    {
        // 1. Se toma la hora una sola vez y se revisa la lectura.
        var ahoraEnUtc = reloj.GetUtcNow();
        var placaLeida = Placa.Normalizar(placa);
        var errorDeLectura = RevisarLectura(placaLeida, confianza, carril);

        if (errorDeLectura != null)
        {
            return Rechazar(errorDeLectura);
        }

        // 2. Una lectura con confianza menor que el umbral de la sede pasa a la caja para validar la placa (F-06).
        var configuracion = await sedes.ObtenerAsync(cancelacion);

        if (confianza < configuracion.UmbralConfianza)
        {
            return Rechazar(ConfianzaBaja);
        }

        // 3. RN-06: cualquier cobro pendiente impide salir, también uno de un día operativo anterior.
        // La placa no necesita un formato reconocido, porque el vehículo pudo entrar por ingreso manual.
        var placaDelVehiculo = Placa.Crear(placaLeida);
        var pendiente = await cobros.SumarPendientesAsync(placaDelVehiculo, sede.Id, cancelacion);

        if (pendiente > 0)
        {
            return Rechazar(NoAbrePorCobroPendiente(pendiente));
        }

        // 4. La barrera abre, con el cobro del día pagado o sin cobro, y se registra la salida.
        var momento = await calendario.UbicarAsync(ahoraEnUtc, cancelacion);
        var pagado = await cobros.ExistePagadoDelDiaAsync(placaDelVehiculo, sede.Id, momento.DiaOperativo, cancelacion);
        var tipoDeVehiculo = ClasificadorDeVehiculo.Clasificar(placaDelVehiculo) ?? TipoDeVehiculo.NoClasificado;
        var salida = EventoDeAcceso.RegistrarSalida(placaDelVehiculo, tipoDeVehiculo, sede.Id, carril, confianza, ahoraEnUtc);
        await eventos.AgregarAsync(salida, cancelacion);

        // 5. Se responde con la hora de la sede, que es la que ve el operador.
        var registrada = new SalidaRegistrada(
            salida.Id,
            salida.Placa.Valor,
            momento.HoraLocal,
            salida.Carril,
            salida.Confianza,
            salida.TipoDeVehiculo);
        var motivo = pagado ? AbreConCobroPagado : AbreSinCobro;

        return new ResultadoDeSalida(true, motivo, registrada);
    }

    // Las mismas reglas de lectura del carril de entrada. Devuelve el motivo del rechazo, o null si la lectura sirve.
    private static string? RevisarLectura(string placa, int confianza, int carril)
    {
        if (placa.Length == 0)
        {
            return LecturaValida.PlacaObligatoria;
        }

        if (placa.Length > Placa.LargoMaximo)
        {
            return $"La placa no puede tener más de {Placa.LargoMaximo} caracteres";
        }

        if (confianza < EventoDeAcceso.ConfianzaMinima || confianza > EventoDeAcceso.ConfianzaMaxima)
        {
            return LecturaValida.ConfianzaFueraDeRango;
        }

        if (carril < 1)
        {
            return "El carril debe ser 1 o mayor";
        }

        return null;
    }

    private static ResultadoDeSalida Rechazar(string motivo)
    {
        return new ResultadoDeSalida(false, motivo, null);
    }
}
