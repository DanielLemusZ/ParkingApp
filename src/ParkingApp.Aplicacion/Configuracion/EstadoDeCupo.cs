namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>
/// Ocupación de un tipo de vehículo frente a su cupo.
/// </summary>
/// <param name="Cupo">Cupo configurado en la sede.</param>
/// <param name="Ocupados">Vehículos de ese tipo que están adentro.</param>
public sealed record EstadoDeCupo(int Cupo, int Ocupados)
{
    /// <summary>
    /// Porcentaje desde el que la ocupación se marca en ámbar (estrictamente mayor), según el mockup.
    /// </summary>
    public const int PorcentajeAlto = 80;

    /// <summary>
    /// Cupos que quedan; nunca negativo.
    /// </summary>
    public int Libres => Math.Max(0, Cupo - Ocupados);

    /// <summary>
    /// Vehículos por encima del cupo, por ejemplo si se bajó el cupo con vehículos adentro.
    /// </summary>
    public int Excedente => Math.Max(0, Ocupados - Cupo);

    /// <summary>
    /// Porcentaje ocupado, de 0 a 100, redondeado hacia abajo para no marcar 100 % antes de tiempo.
    /// Un cupo de 0 cuenta como lleno.
    /// </summary>
    public int Porcentaje => Cupo <= 0 ? 100 : Math.Min(100, Ocupados * 100 / Cupo);

    /// <summary>
    /// Nivel para el color. Lleno usa la misma regla que el carril (<c>ocupados &gt;= cupo</c>).
    /// </summary>
    public NivelDeOcupacion Nivel => Ocupados >= Cupo
        ? NivelDeOcupacion.Lleno
        : Porcentaje > PorcentajeAlto ? NivelDeOcupacion.Alto : NivelDeOcupacion.Normal;
}
