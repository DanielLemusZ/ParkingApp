using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Reportes;

/// <summary>
/// Puerto para consultar los reportes directamente en la base de datos.
/// </summary>
public interface IRepositorioConsultas
{
    /// <summary>
    /// Consulta los accesos filtrados desde la base de datos.
    /// </summary>
    Task<List<EventoDeAcceso>> ConsultarAccesosAsync(DateTimeOffset? fechaInicioUtc, DateTimeOffset? fechaFinUtc, string? placaNormalizada);
}