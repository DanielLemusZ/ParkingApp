using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ParkingApp.Aplicacion.Identidad;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Dominio.Accesos;
using ParkingApp.Dominio.Configuracion;

namespace ParkingApp.Web.Controllers;

/// <summary>
/// API de consulta de las tarifas vigentes. Es de configuración, así que solo la usa el Administrador (F-10):
/// sin sesión responde 401 y con el rol Operador responde 403.
/// </summary>
[Authorize(Roles = Roles.Administrador)]
[ApiController]
[Route("api/tarifas")]
public sealed class TarifasController : ControllerBase
{
    private readonly AdministrarTarifas administrarTarifas;

    /// <summary>
    /// Crea el controlador con el caso de uso de las tarifas, el mismo que usa la pantalla.
    /// </summary>
    /// <param name="administrarTarifas">Caso de uso de las tarifas.</param>
    public TarifasController(AdministrarTarifas administrarTarifas)
    {
        this.administrarTarifas = administrarTarifas;
    }

    /// <summary>
    /// Lista las tarifas vigentes hoy en la sede, una por tipo de vehículo y franja.
    /// </summary>
    /// <param name="cancelacion">Se cancela si el cliente cierra la conexión.</param>
    /// <returns>Las tarifas vigentes.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TarifaVigenteHttp>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TarifaVigenteHttp>>> ListarVigentesAsync(CancellationToken cancelacion)
    {
        var tarifas = await administrarTarifas.ConsultarVigentesAsync(administrarTarifas.FechaActualSede(), cancelacion);

        // La API responde con su propio DTO y no con la entidad del dominio, para no exponer su forma interna.
        var respuesta = tarifas
            .Select(tarifa => new TarifaVigenteHttp(tarifa.TipoDeVehiculo, tarifa.Franja, tarifa.Valor, tarifa.VigenteDesde))
            .ToList();

        return Ok(respuesta);
    }
}

/// <summary>
/// Tarifa vigente, como la responde la API.
/// </summary>
/// <param name="TipoDeVehiculo">Tipo de vehículo.</param>
/// <param name="Franja">Franja tarifaria.</param>
/// <param name="Valor">Valor en pesos colombianos.</param>
/// <param name="VigenteDesde">Día desde el que aplica.</param>
public sealed record TarifaVigenteHttp(TipoDeVehiculo TipoDeVehiculo, FranjaTarifaria Franja, decimal Valor, DateOnly VigenteDesde);
