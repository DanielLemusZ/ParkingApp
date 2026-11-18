using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Repositorio en memoria para probar la configuración de tarifas.
/// </summary>
internal sealed class RepositorioTarifasEnMemoria : IRepositorioTarifas
{
    private readonly List<Tarifa> tarifas = new List<Tarifa>();

    public IReadOnlyList<Tarifa> Tarifas => tarifas;

    public Task<IReadOnlyList<Tarifa>> ObtenerVigentesAsync(DateOnly fecha, CancellationToken cancelacion = default)
    {
        IReadOnlyList<Tarifa> resultado = tarifas.Where(tarifa => tarifa.EstaVigenteEn(fecha)).ToList();
        return Task.FromResult(resultado);
    }

    public Task<Tarifa?> ObtenerVigenteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        var tarifa = tarifas.SingleOrDefault(actual =>
            actual.TipoDeVehiculo == tipoDeVehiculo && actual.Franja == franja && actual.EstaVigenteEn(fecha));
        return Task.FromResult(tarifa);
    }

    public Task<Tarifa?> ObtenerIniciadaAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        var tarifa = tarifas.SingleOrDefault(actual =>
            actual.TipoDeVehiculo == tipoDeVehiculo && actual.Franja == franja && actual.VigenteDesde == fecha);
        return Task.FromResult(tarifa);
    }

    public Task<Tarifa?> ObtenerSiguienteAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        DateOnly fecha,
        CancellationToken cancelacion = default)
    {
        var tarifa = tarifas
            .Where(actual => actual.TipoDeVehiculo == tipoDeVehiculo &&
                actual.Franja == franja &&
                actual.VigenteDesde > fecha)
            .MinBy(actual => actual.VigenteDesde);
        return Task.FromResult(tarifa);
    }

    public Task<bool> AgregarAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        if (tarifas.Any(actual => actual.TipoDeVehiculo == tarifa.TipoDeVehiculo &&
            actual.Franja == tarifa.Franja &&
            actual.VigenteDesde == tarifa.VigenteDesde))
        {
            return Task.FromResult(false);
        }

        tarifas.Add(tarifa);
        return Task.FromResult(true);
    }

    public Task GuardarCambiosAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        return Task.CompletedTask;
    }
}
