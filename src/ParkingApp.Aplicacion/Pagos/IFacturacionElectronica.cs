namespace ParkingApp.Aplicacion.Pagos;

/// <summary>
/// Interfaz para solicitar la factura electrónica a un proveedor externo.
/// </summary>
public interface IFacturacionElectronica
{
    /// <summary>
    /// Solicita la generación de una factura electrónica para un pago registrado.
    /// </summary>
    /// <param name="valor">El valor total del pago.</param>
    /// <param name="fecha">La fecha y hora en que se realizó el pago.</param>
    /// <param name="concepto">El concepto o descripción del pago.</param>
    /// <param name="referencia">La referencia única del pago.</param>
    /// <param name="cancelacion">Token para cancelar la operación asíncrona.</param>
    /// <returns>Una tarea asíncrona que representa la operación de solicitud.</returns>
    Task SolicitarFacturaAsync(decimal valor, DateTimeOffset fecha, string concepto, string referencia, CancellationToken cancelacion = default);
}
