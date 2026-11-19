using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Dominio.Configuracion;

/// <summary>
/// Versión de una tarifa para un tipo de vehículo y una franja.
/// </summary>
public sealed class Tarifa
{
    /// <summary>
    /// Motivo cuando el valor de una tarifa es negativo.
    /// </summary>
    public const string ValorNegativo = "El valor no puede ser negativo.";

    /// <summary>
    /// Motivo cuando la tarifa nocturna no es menor que la diurna.
    /// </summary>
    public const string NocturnaMayorQueDiurna = "La tarifa nocturna debe ser menor que la diurna.";

    /// <summary>
    /// Crea una versión nueva de una tarifa.
    /// </summary>
    /// <param name="tipoDeVehiculo">Tipo de vehículo al que aplica.</param>
    /// <param name="franja">Franja a la que aplica.</param>
    /// <param name="valor">Valor en pesos colombianos, mayor o igual a cero.</param>
    /// <param name="vigenteDesde">Día desde el que aplica esta versión.</param>
    /// <exception cref="ArgumentOutOfRangeException">El tipo, franja o valor están fuera de rango.</exception>
    public Tarifa(TipoDeVehiculo tipoDeVehiculo, FranjaTarifaria franja, decimal valor, DateOnly vigenteDesde)
    {
        if (tipoDeVehiculo is not TipoDeVehiculo.Carro and not TipoDeVehiculo.Moto)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoDeVehiculo));
        }

        if (!Enum.IsDefined(franja))
        {
            throw new ArgumentOutOfRangeException(nameof(franja));
        }

        if (valor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), valor, ValorNegativo);
        }

        Id = Guid.CreateVersion7();
        TipoDeVehiculo = tipoDeVehiculo;
        Franja = franja;
        Valor = valor;
        VigenteDesde = vigenteDesde;
    }

    // Permite reconstruir una tarifa ya guardada.
    private Tarifa()
    {
    }

    /// <summary>
    /// Identificador de la versión de la tarifa.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Tipo de vehículo al que aplica.
    /// </summary>
    public TipoDeVehiculo TipoDeVehiculo { get; private set; }

    /// <summary>
    /// Franja a la que aplica.
    /// </summary>
    public FranjaTarifaria Franja { get; private set; }

    /// <summary>
    /// Valor en pesos colombianos.
    /// </summary>
    public decimal Valor { get; private set; }

    /// <summary>
    /// Primer día de vigencia, inclusive.
    /// </summary>
    public DateOnly VigenteDesde { get; private set; }

    /// <summary>
    /// Último día de vigencia, inclusive. Es nulo para la versión actual o futura abierta.
    /// </summary>
    public DateOnly? VigenteHasta { get; private set; }

    /// <summary>
    /// Indica si esta versión aplica en una fecha determinada.
    /// </summary>
    /// <param name="fecha">Fecha que se consulta.</param>
    /// <returns><see langword="true"/> si la fecha pertenece al periodo de vigencia.</returns>
    public bool EstaVigenteEn(DateOnly fecha)
    {
        return fecha >= VigenteDesde && (VigenteHasta is null || fecha <= VigenteHasta.Value);
    }

    /// <summary>
    /// Valida el valor respecto a la tarifa vigente de la otra franja para el mismo vehículo.
    /// </summary>
    /// <param name="franja">Franja de la tarifa que se desea guardar.</param>
    /// <param name="valor">Valor que se desea guardar.</param>
    /// <param name="tarifaComplementaria">Tarifa vigente para la otra franja, si existe.</param>
    /// <returns>El motivo de rechazo o <see langword="null"/> cuando el valor es válido.</returns>
    public static string? ValidarValor(FranjaTarifaria franja, decimal valor, Tarifa? tarifaComplementaria)
    {
        if (valor < 0)
        {
            return ValorNegativo;
        }

        if (tarifaComplementaria is null || (valor == 0 && tarifaComplementaria.Valor == 0))
        {
            return null;
        }

        var infringeRelacion = franja switch
        {
            FranjaTarifaria.Nocturna => valor >= tarifaComplementaria.Valor,
            FranjaTarifaria.Diurna => tarifaComplementaria.Valor >= valor,
            _ => true,
        };

        return infringeRelacion ? NocturnaMayorQueDiurna : null;
    }

    /// <summary>
    /// Cambia el valor de esta versión después de validar la relación entre franjas.
    /// </summary>
    /// <param name="valor">Nuevo valor.</param>
    /// <param name="tarifaComplementaria">Tarifa vigente de la otra franja, si existe.</param>
    /// <exception cref="ArgumentOutOfRangeException">El valor es negativo.</exception>
    /// <exception cref="InvalidOperationException">El valor incumple la regla entre franjas.</exception>
    public void CambiarValor(decimal valor, Tarifa? tarifaComplementaria)
    {
        var motivo = ValidarValor(Franja, valor, tarifaComplementaria);

        if (motivo == ValorNegativo)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), valor, motivo);
        }

        if (motivo is not null)
        {
            throw new InvalidOperationException(motivo);
        }

        Valor = valor;
    }

    /// <summary>
    /// Cierra la vigencia de esta versión.
    /// </summary>
    /// <param name="ultimoDia">Último día en que esta versión aplica.</param>
    /// <exception cref="ArgumentOutOfRangeException">El día final es anterior al inicio de vigencia.</exception>
    public void FinalizarVigencia(DateOnly ultimoDia)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ultimoDia, VigenteDesde);
        VigenteHasta = ultimoDia;
    }
}
