using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de cobros en memoria, para probar los casos de uso sin base de datos.
/// Imita la restricción única de placa, sede y día operativo (RN-04).
/// </summary>
internal sealed class RepositorioCobrosEnMemoria : IRepositorioCobros
{
    private readonly List<Cobro> cobros = new List<Cobro>();

    public IReadOnlyList<Cobro> Cobros => cobros;

    /// <summary>
    /// Simula dos ingresos simultáneos: la consulta no ve el cobro que otra solicitud acaba de guardar,
    /// pero al guardar sí se aplica la restricción única, como en PostgreSQL.
    /// </summary>
    public bool OtraSolicitudGuardoElCobroDespuesDeLaConsulta { get; set; }

    public Task<bool> ExisteDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        var existe = !OtraSolicitudGuardoElCobroDespuesDeLaConsulta && Existe(placa, sedeId, diaOperativo);
        return Task.FromResult(existe);
    }

    public Task<bool> AgregarAsync(Cobro cobro, CancellationToken cancelacion = default)
    {
        if (Existe(cobro.Placa, cobro.SedeId, cobro.DiaOperativo))
        {
            return Task.FromResult(false);
        }

        cobros.Add(cobro);
        return Task.FromResult(true);
    }

    private bool Existe(Placa placa, Guid sedeId, DateOnly diaOperativo)
    {
        return cobros.Any(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DiaOperativo == diaOperativo);
    }
}
