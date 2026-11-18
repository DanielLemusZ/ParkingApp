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

    public Task AgregarVersionAsync(Tarifa tarifa, CancellationToken cancelacion = default)
    {
        var siguiente = tarifas
            .Where(actual => actual.TipoDeVehiculo == tarifa.TipoDeVehiculo &&
                actual.Franja == tarifa.Franja &&
                actual.VigenteDesde > tarifa.VigenteDesde)
            .MinBy(actual => actual.VigenteDesde);
        var anterior = tarifas
            .Where(actual => actual.TipoDeVehiculo == tarifa.TipoDeVehiculo &&
                actual.Franja == tarifa.Franja &&
                actual.VigenteDesde < tarifa.VigenteDesde &&
                actual.EstaVigenteEn(tarifa.VigenteDesde))
            .MaxBy(actual => actual.VigenteDesde);

        anterior?.FinalizarVigencia(tarifa.VigenteDesde.AddDays(-1));

        if (siguiente is not null)
        {
            tarifa.FinalizarVigencia(siguiente.VigenteDesde.AddDays(-1));
        }

        tarifas.Add(tarifa);
        return Task.CompletedTask;
    }
}
