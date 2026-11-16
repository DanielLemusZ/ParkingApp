using System.Text;

namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Placa de un vehículo, normalizada: en mayúsculas, sin espacios ni guiones.
/// </summary>
public sealed record Placa
{
    /// <summary>
    /// Cantidad máxima de caracteres de una placa normalizada.
    /// </summary>
    public const int LargoMaximo = 10;

    private Placa(string valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Texto de la placa normalizada, por ejemplo <c>ABC123</c>.
    /// </summary>
    public string Valor { get; }

    /// <summary>
    /// Normaliza el texto leído de una placa: lo pasa a mayúsculas y le quita los espacios y los guiones.
    /// </summary>
    /// <param name="texto">Texto leído, por ejemplo <c> abc-123 </c>.</param>
    /// <returns>El texto normalizado, por ejemplo <c>ABC123</c>. Puede quedar vacío.</returns>
    public static string Normalizar(string? texto)
    {
        if (texto == null)
        {
            return string.Empty;
        }

        var normalizada = new StringBuilder();

        foreach (var caracter in texto)
        {
            var esEspacio = char.IsWhiteSpace(caracter);
            var esGuion = caracter == '-';

            if (!esEspacio && !esGuion)
            {
                normalizada.Append(char.ToUpperInvariant(caracter));
            }
        }

        return normalizada.ToString();
    }

    /// <summary>
    /// Crea una placa a partir del texto leído, ya normalizado o no.
    /// </summary>
    /// <param name="texto">Texto leído de la placa.</param>
    /// <returns>La placa normalizada.</returns>
    /// <exception cref="ArgumentException">La placa queda vacía o supera el largo máximo.</exception>
    public static Placa Crear(string? texto)
    {
        var valor = Normalizar(texto);

        if (valor.Length == 0)
        {
            throw new ArgumentException("La placa es obligatoria", nameof(texto));
        }

        if (valor.Length > LargoMaximo)
        {
            throw new ArgumentException($"La placa no puede tener más de {LargoMaximo} caracteres", nameof(texto));
        }

        return new Placa(valor);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Valor;
    }
}
