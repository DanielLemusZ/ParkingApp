namespace ParkingApp.Aplicacion.Configuracion;

/// <summary>Caso de uso para consultar y actualizar la configuración del parqueadero.</summary>
public sealed class ConfigurarSede
{
    private readonly IRepositorioSedes repositorio;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="repositorio">Acceso a la sede y a la ocupación actual.</param>
    public ConfigurarSede(IRepositorioSedes repositorio)
    {
        this.repositorio = repositorio;
    }

    /// <summary>Consulta la configuración y la ocupación actual por tipo de vehículo.</summary>
    public async Task<DatosDeConfiguracionSede> ConsultarAsync(CancellationToken cancelacion = default)
    {
        var sede = await repositorio.ObtenerAsync(cancelacion);
        var ocupacion = await repositorio.ConsultarOcupacionAsync(cancelacion);
        return new DatosDeConfiguracionSede(
            sede.InicioFranjaDiurna,
            sede.InicioFranjaNocturna,
            sede.CupoCarros,
            sede.CupoMotos,
            sede.UmbralConfianza,
            ocupacion);
    }

    /// <summary>Valida y guarda los horarios, cupos y umbral de confianza.</summary>
    /// <returns><see langword="null"/> si se guardó; de lo contrario, el motivo de rechazo.</returns>
    public async Task<string?> GuardarAsync(
        TimeOnly inicioDiurno,
        TimeOnly inicioNocturno,
        int cupoCarros,
        int cupoMotos,
        int umbralConfianza,
        CancellationToken cancelacion = default)
    {
        var sede = await repositorio.ObtenerAsync(cancelacion);
        var error = sede.Configurar(inicioDiurno, inicioNocturno, cupoCarros, cupoMotos, umbralConfianza);
        if (error is not null)
        {
            return error;
        }

        await repositorio.GuardarCambiosAsync(sede, cancelacion);
        return null;
    }
}
