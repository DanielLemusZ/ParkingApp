namespace ParkingApp.Dominio.Configuracion;

/// <summary>
/// Sede del parqueadero. Sus ingresos, salidas y cobros se calculan en la zona horaria de la sede.
/// </summary>
public sealed class Sede
{
    /// <summary>Hora de inicio de la franja diurna.</summary>
    public TimeOnly InicioFranjaDiurna { get; private set; } = new(6, 0);

    /// <summary>Hora de inicio de la franja nocturna.</summary>
    public TimeOnly InicioFranjaNocturna { get; private set; } = new(18, 0);

    /// <summary>Cupo máximo de carros.</summary>
    public int CupoCarros { get; private set; } = 50;

    /// <summary>Cupo máximo de motos.</summary>
    public int CupoMotos { get; private set; } = 50;

    /// <summary>Umbral mínimo de confianza aceptado para una lectura.</summary>
    public int UmbralConfianza { get; private set; } = 80;

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

    /// <summary>Actualiza los horarios de franjas, cupos y umbral de confianza.</summary>
    /// <returns>Un mensaje de error, o <see langword="null"/> si se guardaron los cambios.</returns>
    public string? Configurar(TimeOnly inicioDiurno, TimeOnly inicioNocturno, int cupoCarros, int cupoMotos, int umbralConfianza)
    {
        if (inicioDiurno == inicioNocturno) return "Las franjas no pueden empezar a la misma hora.";
        if (cupoCarros < 0 || cupoMotos < 0) return "El cupo debe ser un número entero mayor o igual a 0.";
        if (umbralConfianza is < 0 or > 100) return "El umbral debe estar entre 0 y 100.";
        InicioFranjaDiurna = inicioDiurno;
        InicioFranjaNocturna = inicioNocturno;
        CupoCarros = cupoCarros;
        CupoMotos = cupoMotos;
        UmbralConfianza = umbralConfianza;
        return null;
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
