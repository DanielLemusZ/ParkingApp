namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Resultado de preparar un pago con la pasarela.
/// </summary>
/// <param name="Iniciado">Si la pasarela quedó lista para cobrar.</param>
/// <param name="Motivo">Texto para el operador.</param>
/// <param name="UrlDePago">Página donde el conductor paga, o <see langword="null"/> si no se inició.</param>
public sealed record ResultadoDeInicioConPasarela(bool Iniciado, string Motivo, Uri? UrlDePago);

/// <summary>
/// Resultado de confirmar una transacción de la pasarela.
/// </summary>
/// <param name="Registrado">Si el pago quedó registrado.</param>
/// <param name="ConsultarDeNuevo">
/// Si la transacción sigue en proceso o la pasarela no respondió: el operador puede consultar otra vez.
/// </param>
/// <param name="Motivo">Texto para el operador.</param>
/// <param name="Pago">El pago, o <see langword="null"/> si no se registró.</param>
public sealed record ResultadoDeConfirmacionConPasarela(bool Registrado, bool ConsultarDeNuevo, string Motivo, PagoDeLaPlaca? Pago);
