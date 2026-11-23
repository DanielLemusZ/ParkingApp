using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;

namespace ParkingApp.Aplicacion.Accesos.Validaciones;

/// <summary>Detiene el ingreso cuando ya se ocupó el cupo del tipo de vehículo leído.</summary>
public sealed class CupoDisponible : ValidacionDeIngreso
{
    /// <summary>Motivo mostrado cuando todos los cupos del tipo indicado están ocupados.</summary>
    public static string ParqueaderoLleno(TipoDeVehiculo tipoDeVehiculo) => tipoDeVehiculo switch
    {
        TipoDeVehiculo.Carro => "LLENO: carros",
        TipoDeVehiculo.Moto => "LLENO: motos",
        _ => throw new ArgumentOutOfRangeException(nameof(tipoDeVehiculo)),
    };

    private readonly IRepositorioSedes sedes;

    /// <summary>Crea la validación con acceso a los cupos y a la ocupación actuales.</summary>
    /// <param name="sedes">Configuración de la sede y ocupación registrada.</param>
    public CupoDisponible(IRepositorioSedes sedes)
    {
        this.sedes = sedes;
    }

    /// <inheritdoc />
    protected override async Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        var tipo = solicitud.ObtenerTipoDeVehiculo()!.Value;
        var sede = await sedes.ObtenerAsync(cancelacion);
        var ocupacion = await sedes.ConsultarOcupacionAsync(cancelacion);
        var (cantidad, cupo) = tipo switch
        {
            TipoDeVehiculo.Carro => (ocupacion.Carros, sede.CupoCarros),
            TipoDeVehiculo.Moto => (ocupacion.Motos, sede.CupoMotos),
            _ => throw new InvalidOperationException("No hay cupo configurado para este tipo de vehículo."),
        };

        return cantidad >= cupo
            ? ResultadoDeValidacion.Rechazar(ParqueaderoLleno(tipo))
            : ResultadoDeValidacion.Aceptar();
    }
}
