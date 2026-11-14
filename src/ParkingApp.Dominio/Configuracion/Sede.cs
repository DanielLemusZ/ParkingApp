namespace ParkingApp.Dominio.Configuracion;

/// <summary>
/// Sede del parqueadero. Sus ingresos, salidas y cobros se calculan en la zona horaria de la sede.
/// </summary>
public sealed class Sede
{
    /// <summary>
    /// Crea una sede nueva, con un identificador propio.
    /// </summary>
    /// <param name="nombre">Nombre de la sede.</param>
    /// <param name="zonaHoraria">Zona horaria de la sede, por ejemplo <c>America/Bogota</c>.</param>
    /// <exception cref="ArgumentException">El nombre está vacío o la zona horaria no existe.</exception>
    public Sede(string nombre, string zonaHoraria)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("La sede necesita un nombre.", nameof(nombre));
        }

        if (string.IsNullOrWhiteSpace(zonaHoraria) || !TimeZoneInfo.TryFindSystemTimeZoneById(zonaHoraria, out _))
        {
            throw new ArgumentException($"La zona horaria '{zonaHoraria}' no existe.", nameof(zonaHoraria));
        }

        Id = Guid.CreateVersion7();
        Nombre = nombre.Trim();
        ZonaHoraria = zonaHoraria;
    }

    // Permite reconstruir una sede ya guardada.
    private Sede()
    {
    }

    /// <summary>
    /// Identificador de la sede.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre de la sede.
    /// </summary>
    public string Nombre { get; private set; } = string.Empty;

    /// <summary>
    /// Zona horaria de la sede, por ejemplo <c>America/Bogota</c>.
    /// </summary>
    public string ZonaHoraria { get; private set; } = string.Empty;
}
