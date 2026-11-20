using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;
using ParkingApp.Dominio.Pagos;

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

    /// <summary>
    /// Atajo para las pruebas que solo necesitan un cobro pagado: lo paga en efectivo con el dominio, sin pasar por la caja.
    /// </summary>
    public void PagarEnEfectivo(Cobro cobro)
    {
        if (!cobros.Contains(cobro))
        {
            throw new InvalidOperationException("El cobro no está guardado en el repositorio.");
        }

        cobro.Pagar(MedioDePago.Efectivo, "caja", Guid.NewGuid(), cobro.FechaHora);
    }

    public Task<decimal> SumarPendientesAsync(Placa placa, Guid sedeId, CancellationToken cancelacion = default)
    {
        var total = cobros
            .Where(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.Estado == EstadoDeCobro.Pendiente)
            .Sum(cobro => cobro.Valor);

        return Task.FromResult(total);
    }

    public Task<bool> ExistePagadoDelDiaAsync(Placa placa, Guid sedeId, DateOnly diaOperativo, CancellationToken cancelacion = default)
    {
        var existe = cobros.Any(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DaPaseVigenteEn(diaOperativo));
        return Task.FromResult(existe);
    }

    public Task<Cobro?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        return Task.FromResult(cobros.FirstOrDefault(cobro => cobro.Id == id));
    }

    public Task<IReadOnlyList<Cobro>> ListarPendientesAsync(Guid sedeId, string placaContiene, CancellationToken cancelacion = default)
    {
        IReadOnlyList<Cobro> pendientes = cobros
            .Where(cobro => cobro.SedeId == sedeId && cobro.Estado == EstadoDeCobro.Pendiente)
            .Where(cobro => cobro.Placa.Valor.Contains(placaContiene, StringComparison.Ordinal))
            .OrderBy(cobro => cobro.FechaHora)
            .ToList();

        return Task.FromResult(pendientes);
    }

    private bool Existe(Placa placa, Guid sedeId, DateOnly diaOperativo)
    {
        return cobros.Any(cobro => cobro.Placa == placa && cobro.SedeId == sedeId && cobro.DiaOperativo == diaOperativo);
    }
}
