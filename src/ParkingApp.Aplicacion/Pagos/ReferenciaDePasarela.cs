using System.Diagnostics.CodeAnalysis;

namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Referencia que la caja le manda a la pasarela, por ejemplo <c>ABC123-0193a1b2c3d4e5f60718293a4b5c6d7e</c>. Lleva la
/// placa y la referencia única del pago, para que al volver de la pasarela se sepa qué pagar sin guardar nada antes.
/// La pasarela firma la referencia junto con el valor, así que no se puede cambiar en el camino.
/// </summary>
public static class ReferenciaDePasarela
{
    /// <summary>
    /// Escribe la referencia de un pago.
    /// </summary>
    /// <param name="placa">Placa normalizada.</param>
    /// <param name="referencia">Referencia única del pago.</param>
    /// <returns>La referencia que se manda a la pasarela.</returns>
    public static string Escribir(string placa, Guid referencia)
    {
        return $"{placa}-{referencia:N}";
    }

    /// <summary>
    /// Lee una referencia que devolvió la pasarela.
    /// </summary>
    /// <param name="texto">Referencia de la transacción.</param>
    /// <param name="placa">Placa del pago.</param>
    /// <param name="referencia">Referencia única del pago.</param>
    /// <returns><see langword="true"/> si la referencia la escribió esta caja.</returns>
    public static bool TryLeer(string? texto, [NotNullWhen(true)] out string? placa, out Guid referencia)
    {
        placa = null;
        referencia = Guid.Empty;

        var separador = texto?.LastIndexOf('-') ?? -1;

        if (texto is null || separador <= 0 ||
            !Guid.TryParseExact(texto[(separador + 1)..], "N", out referencia) || referencia == Guid.Empty)
        {
            referencia = Guid.Empty;
            return false;
        }

        placa = texto[..separador];
        return true;
    }
}
