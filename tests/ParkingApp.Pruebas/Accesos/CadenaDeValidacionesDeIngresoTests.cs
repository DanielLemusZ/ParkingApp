using ParkingApp.Aplicacion.Accesos;

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
