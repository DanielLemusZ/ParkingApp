using Microsoft.Extensions.DependencyInjection;

using ParkingApp.Aplicacion;
using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Aplicacion.Accesos.Validaciones;
using ParkingApp.Aplicacion.Cobros;
using ParkingApp.Aplicacion.Configuracion;
using ParkingApp.Aplicacion.Tarifas;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

public class CadenaDeValidacionesDeIngresoTests
{
    private static readonly SolicitudDeIngreso Lectura = new SolicitudDeIngreso("ABC123", 97, 1, new DateTimeOffset(2026, 11, 16, 13, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task La_cadena_se_detiene_en_el_primer_eslabon_que_rechaza()
    {
        var primero = new EslabonDePrueba(permite: true);
        var segundo = new EslabonDePrueba(permite: false, motivo: "LLENO");
        var tercero = new EslabonDePrueba(permite: true);
        var cadena = CadenaDeValidacionesDeIngreso.Crear(primero, segundo, tercero);

        var resultado = await cadena.ValidarAsync(Lectura, TestContext.Current.CancellationToken);

        Assert.False(resultado.Permite);
        Assert.Equal("LLENO", resultado.Motivo);
        Assert.True(primero.FueRevisado);
        Assert.True(segundo.FueRevisado);
        Assert.False(tercero.FueRevisado);
    }

    [Fact]
    public async Task Si_todos_los_eslabones_aceptan_la_lectura_pasa()
    {
        var primero = new EslabonDePrueba(permite: true);
        var segundo = new EslabonDePrueba(permite: true);
        var cadena = CadenaDeValidacionesDeIngreso.Crear(primero, segundo);

        var resultado = await cadena.ValidarAsync(Lectura, TestContext.Current.CancellationToken);

        Assert.True(resultado.Permite);
        Assert.True(primero.FueRevisado);
        Assert.True(segundo.FueRevisado);
    }

    // Usa la cadena que registra la aplicación, no la copia de los dobles: si allí falta un eslabón, esta prueba falla.
    [Fact]
    public async Task La_cadena_registrada_aplica_todas_las_validaciones()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var carril = new CarrilDeEntradaDePrueba(Lectura.FechaHora, conTarifasSemilla: false);
        carril.ConfigurarCupos(cupoCarros: 0, cupoMotos: 0);

        var servicios = new ServiceCollection();
        servicios.AddSingleton<TimeProvider>(carril.Reloj);
        servicios.AddSingleton<IRepositorioEventosDeAcceso>(carril.Eventos);
        servicios.AddSingleton<IRepositorioLecturasPendientes>(carril.Lecturas);
        servicios.AddSingleton<IRepositorioCobros>(carril.Cobros);
        servicios.AddSingleton<IRepositorioTarifas>(carril.Tarifas);
        servicios.AddSingleton<IRepositorioSedes>(carril.Sedes);
        servicios.AddSingleton<ISedeActual>(carril.Sede);
        servicios.AddSingleton<IHorarioDeOperacionActual>(carril.Horario);
        servicios.AgregarAplicacion();

        await using var proveedor = servicios.BuildServiceProvider();
        await using var alcance = proveedor.CreateAsyncScope();
        var registrar = alcance.ServiceProvider.GetRequiredService<RegistrarIngreso>();

        // Cada caso también falla en los eslabones que siguen, así que además se comprueba el orden.
        var vacia = await registrar.EjecutarAsync("", 10, 1, cancelacion);
        Assert.Equal(LecturaValida.PlacaObligatoria, vacia.Motivo);

        var dudosa = await registrar.EjecutarAsync("CD1234", 60, 1, cancelacion);
        Assert.Equal(ConfianzaSuficiente.ConfianzaBaja, dudosa.Motivo);

        var sinFormato = await registrar.EjecutarAsync("CD1234", 95, 1, cancelacion);
        Assert.Equal(ClasificacionDeVehiculoValida.FormatoNoReconocido, sinFormato.Motivo);

        var lleno = await registrar.EjecutarAsync("ABC123", 95, 1, cancelacion);
        Assert.Equal("LLENO: carros", lleno.Motivo);

        carril.ConfigurarCupos(cupoCarros: 1, cupoMotos: 1);
        var sinTarifa = await registrar.EjecutarAsync("ABC123", 95, 1, cancelacion);
        Assert.Equal("No hay tarifa vigente para carro en franja diurna", sinTarifa.Motivo);
    }

    // Eslabón falso: acepta o rechaza según se le indique, y recuerda si fue revisado.
    private sealed class EslabonDePrueba : ValidacionDeIngreso
    {
        private readonly bool permite;
        private readonly string motivo;

        public EslabonDePrueba(bool permite, string motivo = "")
        {
            this.permite = permite;
            this.motivo = motivo;
        }

        public bool FueRevisado { get; private set; }

        protected override Task<ResultadoDeValidacion> RevisarAsync(SolicitudDeIngreso solicitud, CancellationToken cancelacion)
        {
            FueRevisado = true;

            ResultadoDeValidacion resultado;

            if (permite)
            {
                resultado = ResultadoDeValidacion.Aceptar();
            }
            else
            {
                resultado = ResultadoDeValidacion.Rechazar(motivo);
            }

            return Task.FromResult(resultado);
        }
    }
}
