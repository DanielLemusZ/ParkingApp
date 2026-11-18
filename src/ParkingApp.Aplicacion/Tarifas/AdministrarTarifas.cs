using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Aplicacion.Tarifas;

/// <summary>
/// Consulta y cambia las tarifas del parqueadero, conservando el historial.
/// </summary>
public sealed class AdministrarTarifas
{
    /// <summary>
    /// Motivo cuando la fecha ya fue añadida por otra solicitud concurrente.
    /// </summary>
    public const string FechaDeTarifaDuplicada = "Ya existe una tarifa para esa fecha. Recarga la página e inténtalo de nuevo.";

    private readonly IRepositorioTarifas repositorio;
    private readonly ISedeActual sede;
    private readonly TimeProvider reloj;

    /// <summary>
    /// Crea el caso de uso con el repositorio de tarifas.
    /// </summary>
    /// <param name="repositorio">Repositorio de versiones de tarifas.</param>
    /// <param name="sede">Sede cuya zona horaria determina el día actual.</param>
    /// <param name="reloj">Reloj del sistema, sustituible en las pruebas.</param>
    public AdministrarTarifas(IRepositorioTarifas repositorio, ISedeActual sede, TimeProvider reloj)
    {
        this.repositorio = repositorio;
        this.sede = sede;
        this.reloj = reloj;
    }

    /// <summary>
    /// Obtiene la fecha actual según la zona horaria de la sede.
    /// </summary>
    /// <returns>Fecha local de la sede.</returns>
    public DateOnly FechaActualSede()
    {
        var ahoraEnLaSede = TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), sede.ZonaHoraria);
        return DateOnly.FromDateTime(ahoraEnLaSede.DateTime);
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
        var otraFranja = franja == FranjaTarifaria.Diurna ? FranjaTarifaria.Nocturna : FranjaTarifaria.Diurna;
        var tarifaComplementaria = await repositorio.ObtenerVigenteAsync(tipoDeVehiculo, otraFranja, vigenteDesde, cancelacion);
        var motivo = Tarifa.ValidarValor(franja, valor, tarifaComplementaria);

        if (motivo is not null)
        {
            return new ResultadoDeCambioDeTarifa(false, motivo, null);
        }

        var tarifaConLaMismaFecha = await repositorio.ObtenerIniciadaAsync(tipoDeVehiculo, franja, vigenteDesde, cancelacion);

        if (tarifaConLaMismaFecha is not null)
        {
            tarifaConLaMismaFecha.CambiarValor(valor, tarifaComplementaria);
            await repositorio.GuardarCambiosAsync(tarifaConLaMismaFecha, cancelacion);
            return new ResultadoDeCambioDeTarifa(true, string.Empty, tarifaConLaMismaFecha);
        }

        Tarifa? tarifaAnterior = null;

        if (vigenteDesde > DateOnly.MinValue)
        {
            tarifaAnterior = await repositorio.ObtenerVigenteAsync(
                tipoDeVehiculo,
                franja,
                vigenteDesde.AddDays(-1),
                cancelacion);
        }

        var tarifaSiguiente = await repositorio.ObtenerSiguienteAsync(tipoDeVehiculo, franja, vigenteDesde, cancelacion);
        var tarifa = new Tarifa(tipoDeVehiculo, franja, valor, vigenteDesde);

        if (tarifaAnterior is not null)
        {
            tarifaAnterior.FinalizarVigencia(vigenteDesde.AddDays(-1));
        }

        if (tarifaSiguiente is not null)
        {
            tarifa.FinalizarVigencia(tarifaSiguiente.VigenteDesde.AddDays(-1));
        }

        if (!await repositorio.AgregarAsync(tarifa, cancelacion))
        {
            return new ResultadoDeCambioDeTarifa(false, FechaDeTarifaDuplicada, null);
        }

        return new ResultadoDeCambioDeTarifa(true, string.Empty, tarifa);
    }
}
