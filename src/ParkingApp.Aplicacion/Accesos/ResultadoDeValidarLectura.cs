namespace ParkingApp.Aplicacion.Accesos;

/// <summary>
/// Respuesta de la caja al validar una lectura. Una lectura puede quedar validada aunque la barrera no abra,
/// por ejemplo si el parqueadero está lleno o el vehículo debe un cobro.
/// </summary>
/// <param name="Validada">Si esta solicitud validó la lectura. Si es falso, no cambió nada.</param>
/// <param name="AbreBarrera">Si la barrera se abre.</param>
/// <param name="Motivo">Motivo que se muestra en la caja.</param>
/// <param name="Ingreso">El ingreso registrado, si era una lectura de entrada y la barrera abrió.</param>
/// <param name="Salida">La salida registrada, si era una lectura de salida y la barrera abrió.</param>
public sealed record ResultadoDeValidarLectura(
    bool Validada,
    bool AbreBarrera,
    string Motivo,
    IngresoRegistrado? Ingreso,
    SalidaRegistrada? Salida);
