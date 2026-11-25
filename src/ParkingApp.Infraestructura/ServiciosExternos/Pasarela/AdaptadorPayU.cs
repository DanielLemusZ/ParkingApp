using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Pasarela de pagos con el sandbox de PayU Latam (patrón Adapter). El conductor paga en el WebCheckout de PayU, que
/// recibe un formulario firmado; por eso la caja pasa antes por una pantalla puente que lo envía. Al volver, la caja
/// consulta la orden por su referencia en la API de consultas. No usa webhook, así que no necesita una dirección pública.
/// </summary>
public sealed class AdaptadorPayU : IPasarelaDePagos
{
    /// <summary>
    /// Nombre del cliente HTTP registrado para PayU.
    /// </summary>
    public const string NombreDelCliente = "PayU";

    /// <summary>
    /// Pantalla de la caja que envía el formulario al WebCheckout de PayU.
    /// </summary>
    public const string RutaDelFormulario = "/caja/pasarela/payu";

    private const string Moneda = "COP";

    private readonly IHttpClientFactory fabrica;
    private readonly OpcionesDePayU opciones;
    private readonly ConcurrentDictionary<string, SolicitudDePagoEnPasarela> solicitudes =
        new ConcurrentDictionary<string, SolicitudDePagoEnPasarela>(StringComparer.Ordinal);

    /// <summary>
    /// Crea el adaptador.
    /// </summary>
    /// <param name="fabrica">Fábrica del cliente HTTP de PayU.</param>
    /// <param name="opciones">Credenciales y direcciones de PayU.</param>
    public AdaptadorPayU(IHttpClientFactory fabrica, OpcionesDePayU opciones)
    {
        this.fabrica = fabrica;
        this.opciones = opciones;
    }

    /// <inheritdoc/>
    public bool Disponible => true;

    /// <summary>
    /// Firma de un pago en PayU: SHA-256, en hexadecimal, de
    /// <c>apiKey~merchantId~referenceCode~amount~currency</c>.
    /// </summary>
    /// <param name="apiKey">Llave del comercio.</param>
    /// <param name="merchantId">Identificador del comercio.</param>
    /// <param name="referencia">Referencia del pago.</param>
    /// <param name="valor">Valor, escrito igual que en el formulario.</param>
    /// <returns>La firma en hexadecimal y minúsculas.</returns>
    public static string Firmar(string apiKey, string merchantId, string referencia, string valor)
    {
        var texto = string.Join('~', apiKey, merchantId, referencia, valor, Moneda);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }

    /// <inheritdoc/>
    public async Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Antes de mandar al conductor a PayU se comprueba que responda y que las credenciales sirvan.
        var ping = await EnviarAsync(new { test = opciones.EsSandbox, language = "es", command = "PING", merchant = Comercio() }, cancelacion);

        if (ping is null)
        {
            return new InicioDePagoEnPasarela(false, null);
        }

        using (ping)
        {
            if (!EsExitosa(ping.RootElement))
            {
                return new InicioDePagoEnPasarela(false, null);
            }
        }

        solicitudes[solicitud.Referencia] = solicitud;
        var url = new Uri($"{RutaDelFormulario}?referencia={Uri.EscapeDataString(solicitud.Referencia)}", UriKind.Relative);
        return new InicioDePagoEnPasarela(true, url);
    }

    /// <summary>
    /// Arma el formulario firmado que la pantalla puente envía al WebCheckout de PayU.
    /// </summary>
    /// <param name="referencia">Referencia de un pago iniciado.</param>
    /// <returns>La dirección y los campos del formulario, o <see langword="null"/> si el pago no se inició.</returns>
    public FormularioDePayU? Formulario(string? referencia)
    {
        if (referencia is null || !solicitudes.TryGetValue(referencia, out var solicitud))
        {
            return null;
        }

        var valor = solicitud.Valor.ToString("0.##", CultureInfo.InvariantCulture);
        var campos = new List<KeyValuePair<string, string>>
        {
            new("merchantId", opciones.MerchantId),
            new("accountId", opciones.AccountId),
            new("description", solicitud.Concepto),
            new("referenceCode", solicitud.Referencia),
            new("amount", valor),
            new("tax", "0"),
            new("taxReturnBase", "0"),
            new("currency", Moneda),
            new("signature", Firmar(opciones.ApiKey, opciones.MerchantId, solicitud.Referencia, valor)),
            new("algorithmSignature", "SHA256"),
            new("test", opciones.EsSandbox ? "1" : "0"),
            new("lng", "es"),
            new("buyerEmail", opciones.CorreoDelComprador),
            new("responseUrl", solicitud.UrlDeRetorno.ToString()),
        };

        return new FormularioDePayU(opciones.UrlCheckout, campos);
    }

    /// <summary>
    /// Consulta la orden por su referencia. PayU vuelve a la caja con <c>referenceCode</c>, que es lo que llega aquí.
    /// </summary>
    /// <param name="transaccionId">Referencia de la orden en PayU.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El estado, la transacción y el valor de la última transacción de la orden.</returns>
    public async Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default)
    {
        var consulta = new
        {
            test = opciones.EsSandbox,
            language = "es",
            command = "ORDER_DETAIL_BY_REFERENCE_CODE",
            merchant = Comercio(),
            details = new { referenceCode = transaccionId },
        };

        using var respuesta = await EnviarAsync(consulta, cancelacion);

        if (respuesta is null || !EsExitosa(respuesta.RootElement))
        {
            return SinRespuesta(transaccionId);
        }

        try
        {
            return LeerOrden(respuesta.RootElement, transaccionId);
        }
        catch (Exception error) when (error is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return SinRespuesta(transaccionId);
        }
    }

    // PayU responde: { "code": "SUCCESS", "result": { "payload": [ { "referenceCode": "…", "transactions": [ { "id": "…",
    // "transactionResponse": { "state": "APPROVED", "responseMessage": "…" }, "additionalValues": { "TX_VALUE":
    // { "value": 8000, "currency": "COP" } } } ] } ] } }. Sin orden todavía, "payload" es null.
    private static ConsultaDePagoEnPasarela LeerOrden(JsonElement raiz, string referencia)
    {
        if (!raiz.TryGetProperty("result", out var resultado) ||
            !resultado.TryGetProperty("payload", out var ordenes) ||
            ordenes.ValueKind != JsonValueKind.Array ||
            ordenes.GetArrayLength() == 0)
        {
            return new ConsultaDePagoEnPasarela(EstadoEnPasarela.EnProceso, referencia, referencia, 0m, null);
        }

        var orden = ordenes[ordenes.GetArrayLength() - 1];

        if (!orden.TryGetProperty("transactions", out var transacciones) ||
            transacciones.ValueKind != JsonValueKind.Array ||
            transacciones.GetArrayLength() == 0)
        {
            return new ConsultaDePagoEnPasarela(EstadoEnPasarela.EnProceso, referencia, referencia, 0m, null);
        }

        var transaccion = transacciones[transacciones.GetArrayLength() - 1];
        var respuesta = transaccion.GetProperty("transactionResponse");
        var estado = Texto(respuesta, "state") switch
        {
            "APPROVED" => EstadoEnPasarela.Aprobada,
            "PENDING" => EstadoEnPasarela.EnProceso,
            _ => EstadoEnPasarela.Rechazada,
        };
        var motivo = Texto(respuesta, "responseMessage") ?? Texto(respuesta, "responseCode");
        var valor = Valor(transaccion) ?? Valor(orden) ?? 0m;

        return new ConsultaDePagoEnPasarela(
            estado,
            Texto(transaccion, "id") ?? referencia,
            Texto(orden, "referenceCode") ?? referencia,
            valor,
            motivo);
    }

    private static decimal? Valor(JsonElement elemento)
    {
        return elemento.TryGetProperty("additionalValues", out var adicionales) &&
            adicionales.ValueKind == JsonValueKind.Object &&
            adicionales.TryGetProperty("TX_VALUE", out var valor) &&
            valor.TryGetProperty("value", out var numero) &&
            numero.ValueKind == JsonValueKind.Number
            ? numero.GetDecimal()
            : null;
    }

    private static string? Texto(JsonElement elemento, string propiedad)
    {
        return elemento.TryGetProperty(propiedad, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
    }

    private static bool EsExitosa(JsonElement raiz)
    {
        return raiz.TryGetProperty("code", out var codigo) && codigo.GetString() == "SUCCESS";
    }

    private static ConsultaDePagoEnPasarela SinRespuesta(string transaccionId)
    {
        return new ConsultaDePagoEnPasarela(EstadoEnPasarela.NoDisponible, transaccionId, null, 0m, null);
    }

    private object Comercio()
    {
        return new { apiLogin = opciones.ApiLogin, apiKey = opciones.ApiKey };
    }

    // Devuelve null si PayU no respondió: sin internet, demora, error del servidor o una respuesta que no es JSON.
    private async Task<JsonDocument?> EnviarAsync(object cuerpo, CancellationToken cancelacion)
    {
        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Post, opciones.UrlConsultas)
            {
                Content = JsonContent.Create(cuerpo),
            };
            solicitud.Headers.Accept.ParseAdd("application/json");

            using var respuesta = await fabrica.CreateClient(NombreDelCliente).SendAsync(solicitud, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return null;
            }

            await using var contenido = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            return await JsonDocument.ParseAsync(contenido, cancellationToken: cancelacion);
        }
        catch (Exception error) when (
            error is HttpRequestException or JsonException ||
            (error is TaskCanceledException && !cancelacion.IsCancellationRequested))
        {
            return null;
        }
    }
}

/// <summary>
/// Formulario firmado para el WebCheckout de PayU.
/// </summary>
/// <param name="Accion">Dirección a la que se envía el formulario.</param>
/// <param name="Campos">Campos ocultos del formulario, en orden.</param>
public sealed record FormularioDePayU(Uri Accion, IReadOnlyList<KeyValuePair<string, string>> Campos);
