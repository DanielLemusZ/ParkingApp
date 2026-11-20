using ParkingApp.Aplicacion.Pagos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Facturación electrónica de prueba: guarda las referencias de las facturas solicitadas.
/// </summary>
internal sealed class FacturacionDePrueba : IFacturacionElectronica
{
    private readonly List<string> referencias = new List<string>();

    public IReadOnlyList<string> ReferenciasFacturadas => referencias;

    public Task SolicitarFacturaAsync(decimal valor, DateTimeOffset fecha, string concepto, string referencia, CancellationToken cancelacion = default)
    {
        referencias.Add(referencia);
        return Task.CompletedTask;
    }
}
