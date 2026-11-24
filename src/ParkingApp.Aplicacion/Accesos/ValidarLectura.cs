using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Caso de uso de la caja (F-06): el operador confirma o corrige la placa de una lectura pendiente, y el vehículo sigue
/// el flujo normal de su carril. Al confirmar, la lectura queda validada aunque la barrera no abra: en ese caso el
/// vehículo se vuelve a leer en el carril. Lo usan la pantalla de la caja y la API.
/// </summary>
public sealed class ValidarLectura
{
    /// <summary>
    /// Respuesta cuando la lectura no existe en la sede.
    /// </summary>
    public const string LecturaNoEncontrada = "Lectura no encontrada";

    /// <summary>
    /// Respuesta cuando la lectura ya se validó.
    /// </summary>
    public const string LecturaYaValidada = "La lectura ya fue validada";

    /// <summary>
    /// Respuesta cuando la placa digitada tiene algo distinto de letras y números.
    /// </summary>
    public const string SoloLetrasYNumeros = "La placa solo admite letras y números";

    /// <summary>
    /// Respuesta cuando la placa no tiene un formato reconocido y falta el tipo de vehículo.
    /// </summary>
    public const string FaltaElTipoDeVehiculo = "Indica si es carro o moto";

    /// <summary>
    /// Respuesta cuando la placa escrita o corregida en la salida no figura adentro, por ejemplo por un error al digitarla.
    /// </summary>
    public const string PlacaEscritaNoFiguraAdentro = "No abre: la placa no figura adentro, revisa que esté bien escrita";

    private readonly IRepositorioLecturasPendientes lecturas;
    private readonly RegistrarIngreso registrarIngreso;
    private readonly RegistrarSalida registrarSalida;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con sus dependencias.
    /// </summary>
    /// <param name="lecturas">Donde están las lecturas pendientes.</param>
    /// <param name="registrarIngreso">Flujo del carril de entrada.</param>
    /// <param name="registrarSalida">Flujo del carril de salida.</param>
    /// <param name="sede">Sede donde está la caja.</param>
    /// <param name="reloj">Reloj del sistema; en las pruebas, uno falso.</param>
    public ValidarLectura(
        IRepositorioLecturasPendientes lecturas,
        RegistrarIngreso registrarIngreso,
        RegistrarSalida registrarSalida,
        ISedeActual sede,
        TimeProvider reloj)
    {
        this.lecturas = lecturas;
        this.registrarIngreso = registrarIngreso;
        this.registrarSalida = registrarSalida;
        this.sede = sede;
        this.reloj = reloj;
    }

    /// <summary>
    /// Indica si el operador debe elegir carro o moto: solo en la entrada y si el formato de la placa no se reconoce.
    /// </summary>
    /// <param name="tipo">Si la lectura es de entrada (ingreso) o de salida.</param>
    /// <param name="placa">Placa que confirma o digita el operador.</param>
    /// <returns><see langword="true"/> si hace falta el tipo de vehículo.</returns>
    public static bool NecesitaTipoDeVehiculo(TipoDeEvento tipo, string? placa)
    {
        if (tipo != TipoDeEvento.Ingreso)
        {
            return false;
        }

        var normalizada = Placa.Normalizar(placa);

        // Una placa vacía, muy larga o con símbolos se rechaza por eso, sin pedir el tipo.
        if (RevisarPlaca(normalizada) != null)
        {
            return false;
        }

        return ClasificadorDeVehiculo.Clasificar(Placa.Crear(normalizada)) is null;
    }

    /// <summary>
    /// Valida una lectura pendiente y, con la placa validada, registra el ingreso o la salida si la barrera puede abrir.
    /// </summary>
    /// <param name="lecturaId">Lectura que se valida.</param>
    /// <param name="placa">Placa confirmada o digitada, sin normalizar.</param>
    /// <param name="tipoIndicado">Carro o moto, si el formato de la placa no se reconoce.</param>
    /// <param name="operador">Operador que valida.</param>
    /// <param name="cancelacion">Permite cancelar la operación mientras la lectura siga pendiente.</param>
    /// <returns>Si se validó, si la barrera abre y el motivo.</returns>
    /// <exception cref="ArgumentException">Falta el operador.</exception>
    public async Task<ResultadoDeValidarLectura> EjecutarAsync(
        Guid lecturaId,
        string? placa,
        TipoDeVehiculo? tipoIndicado,
        string operador,
        CancellationToken cancelacion = default)
    {
        // 1. Solo se valida una lectura pendiente de esta sede.
        var lectura = await lecturas.ObtenerAsync(lecturaId, cancelacion);

        if (lectura is null || lectura.SedeId != sede.Id)
        {
            return Rechazar(LecturaNoEncontrada);
        }

        if (lectura.Estado != EstadoDeLectura.Pendiente)
        {
            return Rechazar(LecturaYaValidada);
        }

        // 2. Un dato mal digitado no cierra la lectura: el operador lo corrige y vuelve a confirmar.
        var placaDigitada = Placa.Normalizar(placa);
        var errorDePlaca = RevisarPlaca(placaDigitada);

        if (errorDePlaca != null)
        {
            return Rechazar(errorDePlaca);
        }

        var esCarroOMoto = tipoIndicado is TipoDeVehiculo.Carro or TipoDeVehiculo.Moto;

        if (NecesitaTipoDeVehiculo(lectura.Tipo, placaDigitada) && !esCarroOMoto)
        {
            return Rechazar(FaltaElTipoDeVehiculo);
        }

        var placaValidada = Placa.Crear(placaDigitada);

        // En la salida, una placa corregida que no figura adentro (AHF732 en vez de AHF723) abriría "sin cobro" y el
        // vehículo real saldría sin pagar (RN-06). Se trata como un dato mal digitado: la lectura sigue pendiente.
        if (lectura.Tipo == TipoDeEvento.Salida && !await registrarSalida.FiguraAdentroAsync(placaValidada, cancelacion))
        {
            return Rechazar(PlacaEscritaNoFiguraAdentro);
        }

        // 3. Se cierra la lectura antes de abrir: si dos operadores confirman a la vez, solo el primero sigue.
        lectura.Validar(placaValidada, operador, reloj.GetUtcNow());

        if (!await lecturas.GuardarValidacionAsync(lectura, cancelacion))
        {
            return Rechazar(LecturaYaValidada);
        }

        // 4. El vehículo sigue el flujo de su carril. Ya no se cancela: si se cerrara el navegador a la mitad,
        // la lectura quedaría validada sin su ingreso o su salida.
        ResultadoDeValidarLectura resultado;

        if (lectura.Tipo == TipoDeEvento.Ingreso)
        {
            var validacion = new ValidacionDelOperador(operador, tipoIndicado);
            var ingreso = await registrarIngreso.EjecutarLecturaValidadaAsync(
                placaValidada, lectura.Confianza, lectura.Carril, validacion, CancellationToken.None);
            resultado = new ResultadoDeValidarLectura(true, ingreso.AbreBarrera, ingreso.Motivo, ingreso.Ingreso, null);
        }
        else
        {
            var salida = await registrarSalida.EjecutarLecturaValidadaAsync(
                placaValidada, lectura.Confianza, lectura.Carril, operador, CancellationToken.None);
            resultado = new ResultadoDeValidarLectura(true, salida.AbreBarrera, salida.Motivo, null, salida.Salida);
        }

        // 5. Queda el motivo, también cuando la barrera no abre y por eso no hay evento.
        lectura.AnotarResultado(resultado.Motivo);
        await lecturas.GuardarResultadoAsync(lectura, CancellationToken.None);

        return resultado;
    }

    /// <summary>
    /// Registra una placa que el operador escribe en el carril porque la imagen no se pudo leer. No hubo lectura, así
    /// que la confianza queda en 0; el evento queda como lectura manual, con el operador, y sigue el flujo del carril.
    /// En la salida, la placa debe figurar adentro.
    /// </summary>
    /// <param name="sentido">Si es un ingreso o una salida.</param>
    /// <param name="carril">Número del carril.</param>
    /// <param name="placa">Placa escrita, sin normalizar.</param>
    /// <param name="tipoIndicado">Carro o moto, si el formato de la placa no se reconoce en la entrada.</param>
    /// <param name="operador">Operador que la escribe.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Si se registró, si la barrera abre y el motivo.</returns>
    /// <exception cref="ArgumentException">Falta el operador.</exception>
    public async Task<ResultadoDeValidarLectura> RegistrarLecturaManualAsync(
        TipoDeEvento sentido,
        int carril,
        string? placa,
        TipoDeVehiculo? tipoIndicado,
        string operador,
        CancellationToken cancelacion = default)
    {
        if (carril < 1)
        {
            return Rechazar("El carril debe ser 1 o mayor");
        }

        var placaDigitada = Placa.Normalizar(placa);
        var errorDePlaca = RevisarPlaca(placaDigitada);

        if (errorDePlaca != null)
        {
            return Rechazar(errorDePlaca);
        }

        var esCarroOMoto = tipoIndicado is TipoDeVehiculo.Carro or TipoDeVehiculo.Moto;

        if (NecesitaTipoDeVehiculo(sentido, placaDigitada) && !esCarroOMoto)
        {
            return Rechazar(FaltaElTipoDeVehiculo);
        }

        var placaValidada = Placa.Crear(placaDigitada);

        if (sentido == TipoDeEvento.Ingreso)
        {
            var validacion = new ValidacionDelOperador(operador, tipoIndicado);
            var ingreso = await registrarIngreso.EjecutarLecturaValidadaAsync(placaValidada, EventoDeAcceso.ConfianzaMinima, carril, validacion, cancelacion);
            return new ResultadoDeValidarLectura(true, ingreso.AbreBarrera, ingreso.Motivo, ingreso.Ingreso, null);
        }

        // Una placa mal escrita (AHF732 en vez de AHF723) no tiene cobros y abriría "sin cobro": el vehículo real saldría
        // sin pagar (RN-06). Si no figura adentro, se rechaza para que el operador revise lo que escribió.
        if (!await registrarSalida.FiguraAdentroAsync(placaValidada, cancelacion))
        {
            return Rechazar(PlacaEscritaNoFiguraAdentro);
        }

        var salida = await registrarSalida.EjecutarLecturaValidadaAsync(placaValidada, EventoDeAcceso.ConfianzaMinima, carril, operador, cancelacion);
        return new ResultadoDeValidarLectura(true, salida.AbreBarrera, salida.Motivo, null, salida.Salida);
    }

    // Devuelve el motivo del rechazo, o null si la placa sirve.
    private static string? RevisarPlaca(string placa)
    {
        if (placa.Length == 0)
        {
            return LecturaValida.PlacaObligatoria;
        }

        if (!Placa.SoloTieneLetrasYNumeros(placa))
        {
            return SoloLetrasYNumeros;
        }

        if (placa.Length > Placa.LargoMaximo)
        {
            return $"La placa no puede tener más de {Placa.LargoMaximo} caracteres";
        }

        return null;
    }

    private static ResultadoDeValidarLectura Rechazar(string motivo)
    {
        return new ResultadoDeValidarLectura(false, false, motivo, null, null);
    }
}
