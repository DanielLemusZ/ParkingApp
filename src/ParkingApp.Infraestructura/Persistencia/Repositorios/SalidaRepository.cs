using Microsoft.EntityFrameworkCore;
using ParkingApp.Aplicacion.Accesos;
// Importa el espacio de nombres de tu DbContext y Entidades
// using ParkingApp.Dominio.Cobros; (Si RegistroSalida y EstadoCobro están allí)

namespace ParkingApp.Infraestructura.Persistencia.Repositorios;

public class SalidaRepository : ISalidaRepository
{
    private readonly ParkingAppDbContext _contexto;

    public SalidaRepository(ParkingAppDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<decimal?> ObtenerValorCobroPendienteAsync(string placa)
    {
        // Busca cualquier cobro en estado Pendiente para esa placa
        // Ajusta "EstadoCobro.Pendiente" según cómo lo tengan definido en el dominio
        var cobro = await _contexto.Cobros
            .Where(c => c.Placa == placa && c.Estado == EstadoCobro.Pendiente)
            .FirstOrDefaultAsync();

        return cobro?.Valor;
    }

    public async Task<bool> TieneCobroPagadoHoyAsync(string placa)
    {
        // Obtiene la fecha actual (ajustar si manejan zonas horarias específicas)
        var hoy = DateTime.Today;

        return await _contexto.Cobros
            .AnyAsync(c => c.Placa == placa && 
                           c.Estado == EstadoCobro.Pagado && 
                           c.FechaCreacion.Date == hoy);
    }

public async Task RegistrarSalidaAsync(string placa, DateTimeOffset fechaHora, Guid sedeId, int carril, int confianza)
    {
        // 1. Convertir el string primitivo al ValueObject "Placa" de tu dominio.
        // Asumo que tu equipo creó un método como Placa.Crear(placa) o new Placa(placa). Ajusta esta línea a como lo tengan:
        var placaObjeto = Placa.DeTexto(placa); // <- ¡Ojo! Revisa cómo se instancian las placas en tu proyecto.

        // 2. Determinar tipo de vehículo (Asumimos Desconocido o similar porque salió manual, ajusta según tu Enum)
        var tipoVehiculo = TipoDeVehiculo.Carro; // <- Puedes buscar el ingreso previo en BD para saber si era Moto/Carro.

        // 3. Crear el evento de salida usando el método de fábrica
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