namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Qué tan lleno está un tipo de vehículo, con los colores del mockup de <em>Cupos</em>: verde hasta 80 %,
/// ámbar por encima de 80 % y rojo con "LLENO" al llegar al cupo.
/// </summary>
public enum NivelDeOcupacion
{
    /// <summary>Hasta 80 % del cupo.</summary>
    Normal,

    /// <summary>Más de 80 % del cupo, sin llegar a él.</summary>
    Alto,

    /// <summary>La ocupación llegó al cupo: el carril no deja entrar más de ese tipo.</summary>
    Lleno,
}
