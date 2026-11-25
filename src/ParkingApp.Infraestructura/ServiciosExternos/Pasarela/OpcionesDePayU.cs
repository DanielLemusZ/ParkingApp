namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Credenciales y direcciones de PayU Latam. Por defecto, las del sandbox.
/// </summary>
public sealed class OpcionesDePayU
{
    /// <summary>
    /// Página de pago de PayU (WebCheckout), a la que se envía el formulario.
    /// </summary>
    public Uri UrlCheckout { get; init; } = new Uri("https://sandbox.checkout.payulatam.com/ppp-web-gateway-payu/");

    /// <summary>
    /// API de consultas de PayU, donde se busca la orden por su referencia.
    /// </summary>
    public Uri UrlConsultas { get; init; } = new Uri("https://sandbox.api.payulatam.com/reports-api/4.0/service.cgi");

    /// <summary>
    /// Identificador del comercio.
    /// </summary>
    public string MerchantId { get; init; } = string.Empty;

    /// <summary>
    /// Cuenta del comercio para Colombia.
    /// </summary>
    public string AccountId { get; init; } = string.Empty;

    /// <summary>
    /// Llave con la que se firma cada pago y se autentican las consultas.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Usuario de la API de consultas.
    /// </summary>
    public string ApiLogin { get; init; } = string.Empty;

    /// <summary>
    /// Correo del comprador que pide el WebCheckout. La caja no conoce el correo del conductor.
    /// </summary>
    public string CorreoDelComprador { get; init; } = "conductor@parkingapp.local";

    /// <summary>
    /// Si se paga en el sandbox de PayU. Se deduce de la dirección del WebCheckout.
    /// </summary>
    public bool EsSandbox => UrlCheckout.Host.StartsWith("sandbox.", StringComparison.OrdinalIgnoreCase);
}
