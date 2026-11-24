using System.Globalization;

using Microsoft.Extensions.Configuration;

namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Proveedor de la pasarela de pagos.
/// </summary>
public enum ProveedorDePasarela
{
    /// <summary>
    /// Pasarela simulada: no usa internet y el operador elige si el pago se aprueba o se rechaza.
    /// </summary>
    Simulada,

    /// <summary>
    /// Sandbox de Wompi, con llaves de prueba.
    /// </summary>
    Wompi,
}

/// <summary>
/// Configuración de la pasarela de pagos, leída de las variables <c>PASARELA_*</c> y <c>WOMPI_*</c> de <c>.env</c>.
/// </summary>
public sealed class OpcionesDePasarela
{
    /// <summary>
    /// Proveedor de la pasarela. Sin la variable <c>PASARELA_PROVEEDOR</c>, la simulada.
    /// </summary>
    public ProveedorDePasarela Proveedor { get; init; } = ProveedorDePasarela.Simulada;

    /// <summary>
    /// API de Wompi. Por defecto, la del sandbox.
    /// </summary>
    public Uri UrlApi { get; init; } = new Uri("https://sandbox.wompi.co/v1/");

    /// <summary>
    /// Página de pago de Wompi (Web Checkout).
    /// </summary>
    public Uri UrlCheckout { get; init; } = new Uri("https://checkout.wompi.co/p/");

    /// <summary>
    /// Llave pública del comercio; en el sandbox empieza por <c>pub_test_</c>.
    /// </summary>
    public string LlavePublica { get; init; } = string.Empty;

    /// <summary>
    /// Secreto de integridad del comercio, con el que se firma cada pago. Nunca se sube al repositorio.
    /// </summary>
    public string SecretoIntegridad { get; init; } = string.Empty;

    /// <summary>
    /// Si Wompi falla, cobra con la pasarela simulada. Solo para la demo: en producción el simulado aprobaría pagos
    /// sin que entre dinero.
    /// </summary>
    public bool RespaldoSimulado { get; init; }

    /// <summary>
    /// Fallas técnicas seguidas después de las que la caja cobra solo en efectivo.
    /// </summary>
    public int FallasAntesDeSoloEfectivo { get; init; } = 3;

    /// <summary>
    /// Minutos que la caja cobra solo en efectivo antes de volver a intentar con la pasarela.
    /// </summary>
    public int MinutosSinPasarela { get; init; } = 5;

    /// <summary>
    /// Lee la configuración de la pasarela.
    /// </summary>
    /// <param name="configuracion">Configuración, con las variables de entorno ya cargadas.</param>
    /// <returns>Las opciones de la pasarela.</returns>
    /// <exception cref="InvalidOperationException">Se eligió Wompi sin sus llaves, o un valor no es válido.</exception>
    public static OpcionesDePasarela Leer(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var proveedor = configuracion["PASARELA_PROVEEDOR"];
        var opciones = new OpcionesDePasarela
        {
            Proveedor = string.IsNullOrWhiteSpace(proveedor)
                ? ProveedorDePasarela.Simulada
                : Enum.TryParse<ProveedorDePasarela>(proveedor, ignoreCase: true, out var leido)
                    ? leido
                    : throw new InvalidOperationException($"PASARELA_PROVEEDOR debe ser Simulada o Wompi, no '{proveedor}'."),
            UrlApi = LeerUrl(configuracion, "WOMPI_URL_API") ?? new Uri("https://sandbox.wompi.co/v1/"),
            UrlCheckout = LeerUrl(configuracion, "WOMPI_URL_CHECKOUT") ?? new Uri("https://checkout.wompi.co/p/"),
            LlavePublica = configuracion["WOMPI_LLAVE_PUBLICA"]?.Trim() ?? string.Empty,
            SecretoIntegridad = configuracion["WOMPI_SECRETO_INTEGRIDAD"]?.Trim() ?? string.Empty,
            RespaldoSimulado = bool.TryParse(configuracion["PASARELA_RESPALDO_SIMULADO"], out var respaldo) && respaldo,
            FallasAntesDeSoloEfectivo = LeerEnteroPositivo(configuracion, "PASARELA_FALLAS_ANTES_DE_SOLO_EFECTIVO", 3),
            MinutosSinPasarela = LeerEnteroPositivo(configuracion, "PASARELA_MINUTOS_SIN_PASARELA", 5),
        };

        if (opciones.Proveedor == ProveedorDePasarela.Wompi &&
            (opciones.LlavePublica.Length == 0 || opciones.SecretoIntegridad.Length == 0))
        {
            throw new InvalidOperationException(
                "Con PASARELA_PROVEEDOR=Wompi faltan WOMPI_LLAVE_PUBLICA o WOMPI_SECRETO_INTEGRIDAD en el .env.");
        }

        return opciones;
    }

    // La URL base termina en "/" para que las rutas relativas ("transactions/…") se sumen y no la reemplacen.
    private static Uri? LeerUrl(IConfiguration configuracion, string variable)
    {
        var valor = configuracion[variable]?.Trim();

        if (string.IsNullOrEmpty(valor))
        {
            return null;
        }

        return Uri.TryCreate(valor.EndsWith('/') ? valor : valor + "/", UriKind.Absolute, out var url)
            ? url
            : throw new InvalidOperationException($"{variable} no es una dirección válida.");
    }

    private static int LeerEnteroPositivo(IConfiguration configuracion, string variable, int porDefecto)
    {
        var valor = configuracion[variable];

        if (string.IsNullOrWhiteSpace(valor))
        {
            return porDefecto;
        }

        return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero) && numero > 0
            ? numero
            : throw new InvalidOperationException($"{variable} debe ser un número entero mayor que cero.");
    }
}
