namespace ParkingApp.Dominio.Accesos;

/// <summary>
/// Estado de una lectura que pasó a la caja para que el operador la valide.
/// </summary>
public enum EstadoDeLectura
{
    /// <summary>
    /// El operador todavía no la revisa.
    /// </summary>
    Pendiente,

    /// <summary>
    /// El operador ya la confirmó o la corrigió, haya abierto o no la barrera. No se valida otra vez.
    /// </summary>
    Validada,
}
