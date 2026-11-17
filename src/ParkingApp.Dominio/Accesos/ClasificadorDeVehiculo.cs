namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Clasifica vehículos según los formatos de placa reconocidos por el MVP.
/// </summary>
public static class ClasificadorDeVehiculo
{
    /// <summary>
    /// Clasifica una placa normalizada como carro o moto.
    /// </summary>
    /// <param name="placa">Placa normalizada.</param>
    /// <returns>El tipo identificado, o <see langword="null"/> si no coincide con un formato reconocido.</returns>
    public static TipoDeVehiculo? Clasificar(Placa placa)
    {
        ArgumentNullException.ThrowIfNull(placa);

        var valor = placa.Valor;

        if (valor.Length == 6 && SonLetras(valor, 0, 3) && SonDigitos(valor, 3, 3))
        {
            return TipoDeVehiculo.Carro;
        }

        if (valor.Length == 6 && SonLetras(valor, 0, 3) && SonDigitos(valor, 3, 2) && EsLetra(valor[5]))
        {
            return TipoDeVehiculo.Moto;
        }

        return null;
    }

    private static bool SonLetras(string valor, int inicio, int cantidad)
    {
        for (var indice = inicio; indice < inicio + cantidad; indice++)
        {
            if (!EsLetra(valor[indice]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SonDigitos(string valor, int inicio, int cantidad)
    {
        for (var indice = inicio; indice < inicio + cantidad; indice++)
        {
            if (valor[indice] is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool EsLetra(char caracter)
    {
        return caracter is >= 'A' and <= 'Z';
    }
}
