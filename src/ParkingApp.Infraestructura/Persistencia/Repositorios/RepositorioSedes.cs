using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

/// <summary>Consulta y guarda la sede y calcula la ocupación a partir del último evento de cada placa.</summary>
internal sealed class RepositorioSedes : IRepositorioSedes
{
    private readonly ParkingAppDbContext contexto;

    public RepositorioSedes(ParkingAppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public Task<Sede> ObtenerAsync(CancellationToken cancelacion = default)
    {
        return contexto.Sedes.SingleAsync(cancelacion);
    }

    public async Task GuardarCambiosAsync(Sede sede, CancellationToken cancelacion = default)
    {
        await contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<OcupacionActual> ConsultarOcupacionAsync(CancellationToken cancelacion = default)
    {
        var conteos = await contexto.EventosDeAcceso
            .AsNoTracking()
            .Where(ultimo => ultimo.Tipo == TipoDeEvento.Ingreso &&
                !contexto.EventosDeAcceso.Any(eventoPosterior =>
                    eventoPosterior.Placa == ultimo.Placa && eventoPosterior.FechaHora > ultimo.FechaHora))
            .GroupBy(evento => evento.TipoDeVehiculo)
            .Select(grupo => new { Tipo = grupo.Key, Cantidad = grupo.Count() })
            .ToDictionaryAsync(grupo => grupo.Tipo, grupo => grupo.Cantidad, cancelacion);

        return new OcupacionActual(
            conteos.GetValueOrDefault(TipoDeVehiculo.Carro),
            conteos.GetValueOrDefault(TipoDeVehiculo.Moto));
    }
}
