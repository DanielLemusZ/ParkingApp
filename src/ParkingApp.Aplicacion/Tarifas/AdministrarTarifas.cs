using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Tarifas;

/// <summary>
/// Consulta y cambia las tarifas del parqueadero, conservando el historial.
/// </summary>
public sealed class AdministrarTarifas
{
    /// <summary>
    /// Motivo cuando el valor de una tarifa es negativo.
    /// </summary>
    public const string ValorNegativo = "El valor no puede ser negativo.";

    /// <summary>
    /// Motivo cuando la tarifa nocturna supera la diurna.
    /// </summary>
    public const string NocturnaMayorQueDiurna = "La tarifa nocturna debe ser menor que la diurna.";

    private readonly IRepositorioTarifas repositorio;

    /// <summary>
    /// Crea el caso de uso con el repositorio de tarifas.
    /// </summary>
    /// <param name="repositorio">Repositorio de versiones de tarifas.</param>
    public AdministrarTarifas(IRepositorioTarifas repositorio)
    {
        this.repositorio = repositorio;
    }

    /// <summary>
    /// Obtiene las tarifas que aplican en la fecha indicada.
    /// </summary>
    /// <param name="fecha">Fecha que se consulta.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>Tarifas vigentes.</returns>
    public Task<IReadOnlyList<Tarifa>> ConsultarVigentesAsync(DateOnly fecha, CancellationToken cancelacion = default)
    {
        return repositorio.ObtenerVigentesAsync(fecha, cancelacion);
    }

    /// <summary>
    /// Crea una versión nueva si el valor es válido y respeta la relación entre franjas.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo.</param>
    /// <param name="franja">Franja tarifaria.</param>
    /// <param name="valor">Nuevo valor en pesos colombianos.</param>
    /// <param name="vigenteDesde">Primer día de aplicación.</param>
    /// <param name="cancelacion">Permite cancelar la operación.</param>
    /// <returns>El resultado del cambio.</returns>
    public async Task<ResultadoDeCambioDeTarifa> CambiarAsync(
        TipoDeVehiculo tipoDeVehiculo,
        FranjaTarifaria franja,
        decimal valor,
        DateOnly vigenteDesde,
        CancellationToken cancelacion = default)
    {
        if (valor < 0)
        {
            return new ResultadoDeCambioDeTarifa(false, ValorNegativo, null);
        }

        var otraFranja = franja == FranjaTarifaria.Diurna ? FranjaTarifaria.Nocturna : FranjaTarifaria.Diurna;
        var tarifaComplementaria = await repositorio.ObtenerVigenteAsync(tipoDeVehiculo, otraFranja, vigenteDesde, cancelacion);

        if (franja == FranjaTarifaria.Nocturna &&
            tarifaComplementaria is not null &&
            (valor > tarifaComplementaria.Valor || (valor == tarifaComplementaria.Valor && valor != 0)))
        {
            return new ResultadoDeCambioDeTarifa(false, NocturnaMayorQueDiurna, null);
        }

        if (franja == FranjaTarifaria.Diurna &&
            tarifaComplementaria is not null &&
            (tarifaComplementaria.Valor > valor || (tarifaComplementaria.Valor == valor && valor != 0)))
        {
            return new ResultadoDeCambioDeTarifa(false, NocturnaMayorQueDiurna, null);
        }

        var tarifa = new Tarifa(tipoDeVehiculo, franja, valor, vigenteDesde);
        await repositorio.AgregarVersionAsync(tarifa, cancelacion);

        return new ResultadoDeCambioDeTarifa(true, string.Empty, tarifa);
    }
}
