using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Facturación electrónica de prueba: guarda las referencias de las facturas solicitadas, o falla si se le pide.
/// </summary>
internal sealed class FacturacionDePrueba : IFacturacionElectronica
{
    private readonly List<string> referencias = new List<string>();

    public IReadOnlyList<string> ReferenciasFacturadas => referencias;

    /// <summary>
    /// Simula un proveedor de facturas caído: cada solicitud lanza una excepción.
    /// </summary>
    public bool ProveedorCaido { get; set; }

    public Task SolicitarFacturaAsync(decimal valor, DateTimeOffset fecha, string concepto, string referencia, CancellationToken cancelacion = default)
    {
        if (ProveedorCaido)
        {
            throw new HttpRequestException("El proveedor de facturas no responde.");
        }

        referencias.Add(referencia);
        return Task.CompletedTask;
    }
}
