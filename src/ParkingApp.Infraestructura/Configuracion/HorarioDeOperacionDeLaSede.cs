using Microsoft.EntityFrameworkCore;

using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;
using ParkingApp.Infraestructura.Persistencia;

namespace ParkingApp.Infraestructura.Configuracion;

/// <summary>
/// Lee de la base de datos el horario de las franjas que configuró el administrador para la sede.
/// Se consulta una sola vez por alcance, para que la cadena del carril y el caso de uso usen el mismo horario.
/// </summary>
internal sealed class HorarioDeOperacionDeLaSede : IHorarioDeOperacionActual
{
    private readonly ParkingAppDbContext contexto;
    private readonly ISedeActual sede;
    private HorarioDeOperacion? horario;

    public HorarioDeOperacionDeLaSede(ParkingAppDbContext contexto, ISedeActual sede)
    {
        this.contexto = contexto;
        this.sede = sede;
    }

    public async Task<HorarioDeOperacion> ObtenerAsync(CancellationToken cancelacion = default)
    {
        if (horario is null)
        {
            var franjas = await contexto.Sedes
                .AsNoTracking()
                .Where(sedeGuardada => sedeGuardada.Id == sede.Id)
                .Select(sedeGuardada => new { sedeGuardada.InicioFranjaDiurna, sedeGuardada.InicioFranjaNocturna })
                .SingleAsync(cancelacion);

            horario = new HorarioDeOperacion(franjas.InicioFranjaDiurna, franjas.InicioFranjaNocturna);
        }

        return horario;
    }
}
