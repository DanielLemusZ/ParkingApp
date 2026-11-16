using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso del carril de entrada: valida la lectura de la placa y, si pasa todas las validaciones,
/// registra el ingreso y abre la barrera. Lo usan la pantalla del carril y la API.
/// </summary>
public sealed class RegistrarIngreso
{
    /// <summary>
    /// Motivo que se muestra cuando la barrera abre.
    /// </summary>
    public const string IngresoAceptado = "Ingreso registrado";

    private readonly ValidacionDeIngreso validaciones;
    private readonly IRepositorioEventosDeAcceso repositorio;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="validaciones">Primer eslabón de la cadena de validaciones del carril de entrada.</param>
    /// <param name="repositorio">Donde se guardan los ingresos.</param>
    /// <param name="sede">Sede donde corre el carril.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public RegistrarIngreso(
        ValidacionDeIngreso validaciones,
        IRepositorioEventosDeAcceso repositorio,
        ISedeActual sede,
        TimeProvider reloj)
    {
        this.validaciones = validaciones;
        this.repositorio = repositorio;
        this.sede = sede;
        this.reloj = reloj;
    }

    /// <summary>
    /// Registra el ingreso de un vehículo a partir de la lectura de su placa.
    /// </summary>
    /// <param name="placa">Texto leído de la placa, sin normalizar.</param>
    /// <param name="confianza">Confianza de la lectura, de 0 a 100.</param>
    /// <param name="carril">Número del carril de entrada.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si la barrera abre, el motivo y el ingreso registrado.</returns>
    public async Task<ResultadoDeIngreso> EjecutarAsync(
        string? placa,
        int confianza,
        int carril,
        CancellationToken cancelacion = default)
    {
        // 1. Se normaliza la placa y la lectura recorre la cadena de validaciones.
        var solicitud = new SolicitudDeIngreso(Placa.Normalizar(placa), confianza, carril);
        var validacion = await validaciones.ValidarAsync(solicitud, cancelacion);

        if (!validacion.Permite)
        {
            return new ResultadoDeIngreso(false, validacion.Motivo, null);
        }

        // 2. Se registra el ingreso con la hora actual en UTC.
        var placaNormalizada = Placa.Crear(solicitud.Placa);
        var ahoraEnUtc = reloj.GetUtcNow();
        var ingreso = EventoDeAcceso.RegistrarIngreso(placaNormalizada, sede.Id, carril, confianza, ahoraEnUtc);
        await repositorio.AgregarAsync(ingreso, cancelacion);

        // 3. Se responde con la hora de la sede, que es la que ve el operador.
        var horaDeLaSede = TimeZoneInfo.ConvertTime(ingreso.FechaHora, sede.ZonaHoraria);
        var registrado = new IngresoRegistrado(ingreso.Id, ingreso.Placa.Valor, horaDeLaSede, ingreso.Carril, ingreso.Confianza);

        return new ResultadoDeIngreso(true, IngresoAceptado, registrado);
    }
}
