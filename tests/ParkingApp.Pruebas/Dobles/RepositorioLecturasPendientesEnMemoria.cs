using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio de lecturas en memoria, para probar la validación manual sin base de datos.
/// </summary>
internal sealed class RepositorioLecturasPendientesEnMemoria : IRepositorioLecturasPendientes
{
    private readonly List<LecturaPendiente> lecturas = new List<LecturaPendiente>();

    public IReadOnlyList<LecturaPendiente> Lecturas => lecturas;

    /// <summary>
    /// Simula dos operadores a la vez: otro guardó su validación después de que este leyó la lectura.
    /// </summary>
    public bool OtroOperadorValidoPrimero { get; set; }

    public Task AgregarAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        lecturas.Add(lectura);
        return Task.CompletedTask;
    }

    public Task<LecturaPendiente?> ObtenerAsync(Guid id, CancellationToken cancelacion = default)
    {
        LecturaPendiente? encontrada = null;

        foreach (var lectura in lecturas)
        {
            if (lectura.Id == id)
            {
                encontrada = lectura;
            }
        }

        return Task.FromResult(encontrada);
    }

    public Task<IReadOnlyList<LecturaPendiente>> ListarPendientesAsync(Guid sedeId, CancellationToken cancelacion = default)
    {
        var pendientes = new List<LecturaPendiente>();

        foreach (var lectura in lecturas)
        {
            if (lectura.SedeId == sedeId && lectura.Estado == EstadoDeLectura.Pendiente)
            {
                pendientes.Add(lectura);
            }
        }

        return Task.FromResult<IReadOnlyList<LecturaPendiente>>(pendientes);
    }

    public Task<bool> GuardarValidacionAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        return Task.FromResult(!OtroOperadorValidoPrimero);
    }

    public Task GuardarResultadoAsync(LecturaPendiente lectura, CancellationToken cancelacion = default)
    {
        return Task.CompletedTask;
    }
}
