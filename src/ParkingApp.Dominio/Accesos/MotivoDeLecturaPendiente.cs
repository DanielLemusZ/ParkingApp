namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Por qué una lectura necesita que el operador la valide en la caja (F-06).
/// </summary>
public enum MotivoDeLecturaPendiente
{
    /// <summary>
    /// La confianza es menor que el umbral de la sede. Aplica a la entrada y a la salida.
    /// </summary>
    ConfianzaBaja,

    /// <summary>
    /// La placa no tiene un formato de carro o moto. Aplica solo a la entrada.
    /// </summary>
    FormatoNoReconocido,

    /// <summary>
    /// La placa leída en la salida no tiene un ingreso sin su salida: puede ser una lectura incompleta o equivocada.
    /// Aplica solo a la salida.
    /// </summary>
    NoFiguraAdentro,
}
