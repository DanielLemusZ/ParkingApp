using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Infraestructura.ServiciosExternos.Pasarela;

/// <summary>
/// Pasarela de pagos con el sandbox de Wompi (patrón Adapter). El conductor paga en el Web Checkout de Wompi con
/// tarjeta, Nequi o QR; al volver, la caja consulta la transacción. No usa webhook, así que no necesita una dirección
/// pública: basta con que el equipo de la caja tenga internet.
/// </summary>
public sealed class AdaptadorWompi : IPasarelaDePagos
{
    /// <summary>
    /// Nombre del cliente HTTP registrado para Wompi.
    /// </summary>
    public const string NombreDelCliente = "Wompi";

    private const string Moneda = "COP";

    private readonly IHttpClientFactory fabrica;
    private readonly OpcionesDePasarela opciones;

    /// <summary>
    /// Crea el adaptador.
    /// </summary>
    /// <param name="fabrica">Fábrica del cliente HTTP de Wompi.</param>
    /// <param name="opciones">Llaves y direcciones de Wompi.</param>
    public AdaptadorWompi(IHttpClientFactory fabrica, OpcionesDePasarela opciones)
    {
        this.fabrica = fabrica;
        this.opciones = opciones;
    }

    /// <inheritdoc/>
    public bool Disponible => true;

    /// <summary>
    /// Firma de integridad de Wompi: SHA-256, en hexadecimal, de la referencia, el valor en centavos, la moneda y el
    /// secreto de integridad, en ese orden. Wompi rechaza un pago cuya firma no coincide.
    /// </summary>
    /// <param name="referencia">Referencia del pago.</param>
    /// <param name="centavos">Valor en centavos.</param>
    /// <param name="secretoIntegridad">Secreto de integridad del comercio.</param>
    /// <returns>La firma en hexadecimal y minúsculas.</returns>
    public static string Firmar(string referencia, long centavos, string secretoIntegridad)
    {
        var texto = string.Concat(referencia, centavos.ToString(CultureInfo.InvariantCulture), Moneda, secretoIntegridad);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }

    /// <inheritdoc/>
    public async Task<InicioDePagoEnPasarela> IniciarAsync(SolicitudDePagoEnPasarela solicitud, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        // Antes de mandar al conductor a Wompi se comprueba que responda y que la llave sea válida.
        try
        {
            using var respuesta = await Cliente().GetAsync(new Uri($"merchants/{opciones.LlavePublica}", UriKind.Relative), cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return new InicioDePagoEnPasarela(false, null);
            }
        }
        catch (Exception error) when (EsFallaDeRed(error, cancelacion))
        {
            return new InicioDePagoEnPasarela(false, null);
        }

        var centavos = decimal.ToInt64(decimal.Round(solicitud.Valor * 100m, MidpointRounding.AwayFromZero));
        var parametros = new[]
        {
            ("public-key", opciones.LlavePublica),
            ("currency", Moneda),
            ("amount-in-cents", centavos.ToString(CultureInfo.InvariantCulture)),
            ("reference", solicitud.Referencia),
            ("signature:integrity", Firmar(solicitud.Referencia, centavos, opciones.SecretoIntegridad)),
            ("redirect-url", solicitud.UrlDeRetorno.ToString()),
        };
        var consulta = string.Join('&', parametros.Select(p => $"{p.Item1}={Uri.EscapeDataString(p.Item2)}"));

        return new InicioDePagoEnPasarela(true, new Uri($"{opciones.UrlCheckout}?{consulta}"));
    }

    /// <inheritdoc/>
    public async Task<ConsultaDePagoEnPasarela> ConsultarAsync(string transaccionId, CancellationToken cancelacion = default)
    {
        try
        {
            using var respuesta = await Cliente().GetAsync(
                new Uri($"transactions/{Uri.EscapeDataString(transaccionId)}", UriKind.Relative),
                cancelacion);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                return new ConsultaDePagoEnPasarela(EstadoEnPasarela.Rechazada, transaccionId, null, 0m, "Wompi no conoce la transacción");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                return SinRespuesta(transaccionId);
            }

            await using var contenido = await respuesta.Content.ReadAsStreamAsync(cancelacion);
            using var json = await JsonDocument.ParseAsync(contenido, cancellationToken: cancelacion);
            return LeerTransaccion(json.RootElement.GetProperty("data"), transaccionId);
        }
        catch (Exception error) when (EsFallaDeRed(error, cancelacion) || error is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return SinRespuesta(transaccionId);
        }
    }

    // Wompi responde, por ejemplo: { "data": { "id": "…", "status": "APPROVED", "reference": "…",
    // "amount_in_cents": 800000, "status_message": null } }.
    private static ConsultaDePagoEnPasarela LeerTransaccion(JsonElement datos, string transaccionId)
    {
        var estado = datos.GetProperty("status").GetString() switch
        {
            "APPROVED" => EstadoEnPasarela.Aprobada,
            "PENDING" => EstadoEnPasarela.EnProceso,
            _ => EstadoEnPasarela.Rechazada,
        };
        var referencia = datos.GetProperty("reference").GetString();
        var valor = datos.GetProperty("amount_in_cents").GetInt64() / 100m;
        var motivo = datos.TryGetProperty("status_message", out var mensaje) && mensaje.ValueKind == JsonValueKind.String
            ? mensaje.GetString()
            : null;

        return new ConsultaDePagoEnPasarela(estado, datos.GetProperty("id").GetString() ?? transaccionId, referencia, valor, motivo);
    }

    private static ConsultaDePagoEnPasarela SinRespuesta(string transaccionId)
    {
        return new ConsultaDePagoEnPasarela(EstadoEnPasarela.NoDisponible, transaccionId, null, 0m, null);
    }

    // Sin internet, Wompi caído o una demora que vence el tiempo del cliente; una cancelación pedida no es una falla.
    private static bool EsFallaDeRed(Exception error, CancellationToken cancelacion)
    {
        return error is HttpRequestException || (error is TaskCanceledException && !cancelacion.IsCancellationRequested);
    }

    private HttpClient Cliente()
    {
        return fabrica.CreateClient(NombreDelCliente);
    }
}
