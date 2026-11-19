using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Cobros;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>
/// Implementación de persistencia para las operaciones de salida y consulta de cobros.
/// </summary>
public class SalidaRepository : ISalidaRepository
{
    private readonly ParkingAppDbContext _contexto;

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de salidas.
    /// </summary>
    /// <param name="contexto">Contexto de base de datos de la aplicación.</param>
    public SalidaRepository(ParkingAppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<decimal?> ObtenerValorCobroPendienteAsync(string placa)
    {
        var cobro = await _contexto.Cobros
            .Where(c => c.Placa == placa && c.Estado == EstadoCobro.Pendiente)
            .FirstOrDefaultAsync();

        return cobro?.Valor;
    }

    /// <inheritdoc />
    public async Task<bool> TieneCobroPagadoHoyAsync(string placa)
    {
        var hoy = DateTime.Today;

        return await _contexto.Cobros
            .AnyAsync(c => c.Placa == placa &&
                           c.Estado == EstadoCobro.Pagado &&
                           c.FechaCreacion.Date == hoy);
    }

    /// <inheritdoc />
    public async Task RegistrarSalidaAsync(string placa, DateTimeOffset fechaHora, Guid sedeId, int carril, int confianza)
    {
        var placaObjeto = Placa.Crear(placa);
        var tipoVehiculo = TipoDeVehiculo.Carro;

        var eventoSalida = EventoDeAcceso.RegistrarSalida(
            placaObjeto,
            tipoVehiculo,
            sedeId,
            carril,
            confianza,
            fechaHora
        );

        _contexto.EventosDeAcceso.Add(eventoSalida);
        await _contexto.SaveChangesAsync();
    }
}
