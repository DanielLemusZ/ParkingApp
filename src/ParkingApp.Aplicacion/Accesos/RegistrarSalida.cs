using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso del carril de salida (F-05): la barrera abre solo si el vehículo no tiene cobros pendientes (RN-06)
/// y, si abre, registra la salida. Si la lectura es dudosa, o la placa no figura adentro, la deja pendiente para que el
/// operador la valide (F-06). Lo usan la pantalla del carril, la API y la validación de la caja.
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
    public const string ConfianzaBaja = ConfianzaSuficiente.ConfianzaBaja;

    /// <summary>
    /// Motivo cuando la placa leída no figura adentro, por ejemplo por una lectura incompleta: el operador la valida.
    /// </summary>
    public const string NoFiguraAdentro = "No abre: la placa no figura adentro, hay que validar la lectura";

    private readonly IRepositorioEventosDeAcceso eventos;
    private readonly IRepositorioLecturasPendientes lecturas;
    private readonly IRepositorioCobros cobros;
    private readonly IRepositorioSedes sedes;
    private readonly ISedeActual sede;
    private readonly CalendarioOperativo calendario;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="eventos">Donde se guardan las salidas.</param>
    /// <param name="lecturas">Donde quedan las lecturas que debe validar el operador.</param>
    /// <param name="cobros">Donde se consultan los cobros del vehículo.</param>
    /// <param name="sedes">Donde se consulta el umbral de confianza de la sede.</param>
    /// <param name="sede">Sede donde corre el carril.</param>
    /// <param name="calendario">Calcula el día operativo de la salida.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public RegistrarSalida(
        IRepositorioEventosDeAcceso eventos,
        IRepositorioLecturasPendientes lecturas,
        IRepositorioCobros cobros,
        IRepositorioSedes sedes,
        ISedeActual sede,
        CalendarioOperativo calendario,
        TimeProvider reloj)
    {
        this.eventos = eventos;
        this.lecturas = lecturas;
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

        // 2. Una lectura con confianza menor que el umbral de la sede queda pendiente para validar la placa (F-06).
        var configuracion = await sedes.ObtenerAsync(cancelacion);

        var placaDelVehiculo = Placa.Crear(placaLeida);

        if (confianza < configuracion.UmbralConfianza)
        {
            return await DejarPendienteAsync(MotivoDeLecturaPendiente.ConfianzaBaja, ConfianzaBaja, placaDelVehiculo, carril, confianza, ahoraEnUtc, cancelacion);
        }

        // 3. Una placa que no figura adentro puede ser una lectura incompleta o equivocada. Abrir con ella dejaría salir
        // a un vehículo sin revisar sus cobros (RN-06), y el verdadero seguiría ocupando un cupo: el operador la valida.
        if (!await eventos.FiguraAdentroAsync(placaDelVehiculo, sede.Id, cancelacion))
        {
            return await DejarPendienteAsync(MotivoDeLecturaPendiente.NoFiguraAdentro, NoFiguraAdentro, placaDelVehiculo, carril, confianza, ahoraEnUtc, cancelacion);
        }

        return await RevisarCobrosYRegistrarAsync(placaDelVehiculo, confianza, carril, ahoraEnUtc, null, cancelacion);
    }

    /// <summary>
    /// Indica si la placa figura adentro de la sede: tiene un ingreso sin su salida.
    /// </summary>
    /// <param name="placa">Placa del vehículo.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns><see langword="true"/> si la placa figura adentro.</returns>
    public Task<bool> FiguraAdentroAsync(Placa placa, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(placa);

        return eventos.FiguraAdentroAsync(placa, sede.Id, cancelacion);
    }

    /// <summary>
    /// Sigue la salida de una lectura que validó el operador (F-06): revisa los cobros sin volver a revisar la confianza
    /// y, si abre, la salida queda como lectura manual.
    /// </summary>
    /// <param name="placa">Placa que validó el operador.</param>
    /// <param name="confianza">Confianza de la lectura original.</param>
    /// <param name="carril">Número del carril de salida.</param>
    /// <param name="operador">Operador que validó la placa.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si la barrera abre, el motivo y la salida registrada.</returns>
    public Task<ResultadoDeSalida> EjecutarLecturaValidadaAsync(
        Placa placa,
        int confianza,
        int carril,
        string operador,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(placa);

        return RevisarCobrosYRegistrarAsync(placa, confianza, carril, reloj.GetUtcNow(), operador, cancelacion);
    }

    private async Task<ResultadoDeSalida> RevisarCobrosYRegistrarAsync(
        Placa placaDelVehiculo,
        int confianza,
        int carril,
        DateTimeOffset ahoraEnUtc,
        string? validadoPor,
        CancellationToken cancelacion)
    {
        // 4. RN-06: cualquier cobro pendiente impide salir, también uno de un día operativo anterior.
        // La placa no necesita un formato reconocido, porque el vehículo pudo entrar por ingreso manual.
        var pendiente = await cobros.SumarPendientesAsync(placaDelVehiculo, sede.Id, cancelacion);

        if (pendiente > 0)
        {
            return Rechazar(NoAbrePorCobroPendiente(pendiente));
        }

        // 5. La barrera abre, con el cobro del día pagado o sin cobro, y se registra la salida.
        var momento = await calendario.UbicarAsync(ahoraEnUtc, cancelacion);
        var pagado = await cobros.ExistePagadoDelDiaAsync(placaDelVehiculo, sede.Id, momento.DiaOperativo, cancelacion);
        var tipoDeVehiculo = ClasificadorDeVehiculo.Clasificar(placaDelVehiculo) ?? TipoDeVehiculo.NoClasificado;
        var salida = EventoDeAcceso.RegistrarSalida(placaDelVehiculo, tipoDeVehiculo, sede.Id, carril, confianza, ahoraEnUtc, validadoPor);
        await eventos.AgregarAsync(salida, cancelacion);

        // 6. Se responde con la hora de la sede, que es la que ve el operador.
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

    // La barrera no abre y la lectura queda pendiente, para validarla en el carril o en la caja.
    private async Task<ResultadoDeSalida> DejarPendienteAsync(
        MotivoDeLecturaPendiente motivo,
        string texto,
        Placa placa,
        int carril,
        int confianza,
        DateTimeOffset ahoraEnUtc,
        CancellationToken cancelacion)
    {
        var lectura = LecturaPendiente.Registrar(TipoDeEvento.Salida, motivo, placa, sede.Id, carril, confianza, ahoraEnUtc);
        await lecturas.AgregarAsync(lectura, cancelacion);

        var porRevisar = LecturaPorRevisar.Crear(lectura, sede.ZonaHoraria);
        return new ResultadoDeSalida(false, texto, null, PendienteDeValidacion: true, LecturaPendiente: porRevisar);
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

        if (!Placa.SePuedeGuardar(placa))
        {
            return LecturaValida.CaracteresNoValidos;
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
