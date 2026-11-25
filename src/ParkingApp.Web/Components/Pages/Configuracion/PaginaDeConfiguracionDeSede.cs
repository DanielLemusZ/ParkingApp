using Microsoft.AspNetCore.Components;

using ParkingApp.Aplicacion.Configuracion;

namespace ParkingApp.Web.Components.Pages.Configuracion;

/// <summary>
/// Base de las subpáginas <em>Franjas horarias</em>, <em>Cupos</em> y <em>Umbral de lectura</em> (#71). Antes eran un solo
/// formulario; ahora cada subpágina muestra su parte, pero todas cargan y guardan la configuración completa con el mismo
/// caso de uso, <see cref="ConfigurarSede"/>. Así la regla no cambia: solo la presentación.
/// </summary>
public abstract class PaginaDeConfiguracionDeSede : ComponentBase
{
    /// <summary>
    /// Crea un alcance por operación, porque el circuito de Blazor vive mucho tiempo.
    /// </summary>
    [Inject]
    protected IServiceScopeFactory FabricaDeAlcances { get; set; } = default!;

    /// <summary>
    /// Hora de inicio de la franja diurna.
    /// </summary>
    protected TimeSpan? InicioDiurno { get; set; } = new(6, 0, 0);

    /// <summary>
    /// Hora de inicio de la franja nocturna.
    /// </summary>
    protected TimeSpan? InicioNocturno { get; set; } = new(18, 0, 0);

    /// <summary>
    /// Cupo de carros.
    /// </summary>
    protected int CupoCarros { get; set; } = 50;

    /// <summary>
    /// Cupo de motos.
    /// </summary>
    protected int CupoMotos { get; set; } = 50;

    /// <summary>
    /// Confianza mínima de una lectura, de 0 a 100.
    /// </summary>
    protected int Umbral { get; set; } = 80;

    /// <summary>
    /// Carros que están adentro.
    /// </summary>
    protected int OcupacionCarros { get; private set; }

    /// <summary>
    /// Motos que están adentro.
    /// </summary>
    protected int OcupacionMotos { get; private set; }

    /// <summary>
    /// Resultado del último guardado: el error o la confirmación.
    /// </summary>
    protected string Mensaje { get; private set; } = string.Empty;

    /// <summary>
    /// Si el último guardado salió bien.
    /// </summary>
    protected bool Guardado { get; private set; }

    /// <summary>
    /// Si se está guardando, para no enviar dos veces.
    /// </summary>
    protected bool Guardando { get; private set; }

    /// <inheritdoc />
    protected override Task OnInitializedAsync()
    {
        return CargarAsync();
    }

    /// <summary>
    /// Guarda la configuración completa: la parte que cambió esta subpágina y la que ya estaba.
    /// </summary>
    /// <returns>Una tarea que termina al guardar y volver a cargar.</returns>
    protected async Task GuardarAsync()
    {
        if (Guardando)
        {
            return;
        }

        Guardando = true;

        try
        {
            if (InicioDiurno is null || InicioNocturno is null)
            {
                Mensaje = "Debes indicar la hora de inicio de ambas franjas.";
                Guardado = false;
                return;
            }

            await using var alcance = FabricaDeAlcances.CreateAsyncScope();
            var configurarSede = alcance.ServiceProvider.GetRequiredService<ConfigurarSede>();
            Mensaje = await configurarSede.GuardarAsync(
                TimeOnly.FromTimeSpan(InicioDiurno.Value),
                TimeOnly.FromTimeSpan(InicioNocturno.Value),
                CupoCarros,
                CupoMotos,
                Umbral) ?? string.Empty;
            Guardado = string.IsNullOrEmpty(Mensaje);

            if (Guardado)
            {
                Mensaje = CupoCarros <= OcupacionCarros || CupoMotos <= OcupacionMotos
                    ? "La configuración se guardó. Advertencia: el cupo de uno o más tipos ya se alcanzó; el parqueadero aparece LLENO hasta que baje la ocupación."
                    : "La configuración se guardó correctamente.";
                await CargarAsync();
            }
        }
        finally
        {
            Guardando = false;
        }
    }

    private async Task CargarAsync()
    {
        await using var alcance = FabricaDeAlcances.CreateAsyncScope();
        var configurarSede = alcance.ServiceProvider.GetRequiredService<ConfigurarSede>();
        var datos = await configurarSede.ConsultarAsync();
        InicioDiurno = datos.InicioFranjaDiurna.ToTimeSpan();
        InicioNocturno = datos.InicioFranjaNocturna.ToTimeSpan();
        CupoCarros = datos.CupoCarros;
        CupoMotos = datos.CupoMotos;
        Umbral = datos.UmbralConfianza;
        OcupacionCarros = datos.Ocupacion.Carros;
        OcupacionMotos = datos.Ocupacion.Motos;
    }
}
