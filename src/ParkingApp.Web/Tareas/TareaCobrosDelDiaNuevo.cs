using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Web.Tareas;

/// <summary>
/// Genera el cobro del día nuevo de los vehículos que siguen adentro (S-05, D-28): una vez al arrancar, por si la
/// aplicación estaba apagada a las 06:00, y después cada día a las 06:00 de la sede. Solo decide cuándo correr;
/// qué se cobra lo decide el caso de uso <see cref="GenerarCobrosDelDiaNuevo"/>.
/// </summary>
internal sealed partial class TareaCobrosDelDiaNuevo : BackgroundService
{
    private readonly IServiceScopeFactory fabricaDeAlcances;
    private readonly TimeProvider reloj;
    private readonly ISedeActual sede;
    private readonly ILogger<TareaCobrosDelDiaNuevo> registro;

    public TareaCobrosDelDiaNuevo(
        IServiceScopeFactory fabricaDeAlcances,
        TimeProvider reloj,
        ISedeActual sede,
        ILogger<TareaCobrosDelDiaNuevo> registro)
    {
        this.fabricaDeAlcances = fabricaDeAlcances;
        this.reloj = reloj;
        this.sede = sede;
        this.registro = registro;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Criterio 7: al arrancar genera los cobros que falten.
        await CorrerAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TiempoHastaElProximoDia(), reloj, stoppingToken);
            await CorrerAsync(stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cobros del día nuevo: {Generados} generados, {Omitidos} omitidos.")]
    private static partial void CobrosGenerados(ILogger registro, int generados, int omitidos);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron generar los cobros del día nuevo. Se reintenta en la próxima corrida.")]
    private static partial void CobrosNoGenerados(ILogger registro, Exception error);

    // Espera hasta las 06:00 del próximo día operativo, en la zona horaria de la sede.
    private TimeSpan TiempoHastaElProximoDia()
    {
        var ahora = reloj.GetUtcNow();
        var hoy = HorarioDeOperacion.DiaOperativo(TimeZoneInfo.ConvertTime(ahora, sede.ZonaHoraria));
        var espera = HorarioDeOperacion.InicioEnUtc(hoy.AddDays(1), sede.ZonaHoraria) - ahora;

        return espera > TimeSpan.Zero ? espera : TimeSpan.Zero;
    }

    private async Task CorrerAsync(CancellationToken cancelacion)
    {
        try
        {
            // La tarea vive toda la vida de la aplicación y los repositorios son por alcance: un alcance por corrida.
            await using var alcance = fabricaDeAlcances.CreateAsyncScope();
            var casoDeUso = alcance.ServiceProvider.GetRequiredService<GenerarCobrosDelDiaNuevo>();
            var resultado = await casoDeUso.EjecutarAsync(cancelacion);

            CobrosGenerados(registro, resultado.Generados, resultado.Omitidos);
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
#pragma warning disable CA1031 // Se captura cualquier excepción a propósito: una falla (por ejemplo, la base caída)
        // no debe tumbar la aplicación; la próxima corrida genera los cobros que falten.
        catch (Exception error)
#pragma warning restore CA1031
        {
            CobrosNoGenerados(registro, error);
        }
    }
}
