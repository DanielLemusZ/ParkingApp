using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Pruebas.Dobles;

namespace ParkingApp.Pruebas.Accesos;

// Escenarios del issue #15: la imagen se revisa antes de pedirle la placa al lector.
public class LeerPlacaDesdeImagenTests
{
    private static readonly byte[] InicioDePng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    private static readonly byte[] InicioDeJpg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
    private static readonly byte[] InicioDePdf = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 };

    private readonly LectorDePlacasDePrueba lector = new LectorDePlacasDePrueba();
    private readonly FakeTimeProvider reloj = new FakeTimeProvider();
    private readonly LeerPlacaDesdeImagen casoDeUso;

    public LeerPlacaDesdeImagenTests()
    {
        casoDeUso = new LeerPlacaDesdeImagen(lector, reloj);
    }

    [Fact]
    public async Task Una_imagen_PNG_con_placa_devuelve_la_placa_y_su_confianza()
    {
        var lectura = await LeerAsync(InicioDePng);

        Assert.True(lectura.Leida);
        Assert.Equal("ABC123", lectura.Placa);
        Assert.Equal(97, lectura.Confianza);
        Assert.Null(lectura.Falla);
    }

    [Fact]
    public async Task Una_imagen_JPG_tambien_se_acepta()
    {
        lector.Respuesta = LecturaDeImagen.Exitosa("XYZ12D", 99);

        var lectura = await LeerAsync(InicioDeJpg);

        Assert.True(lectura.Leida);
        Assert.Equal("XYZ12D", lectura.Placa);
    }

    [Fact]
    public async Task Sin_placa_en_la_imagen_se_informa_el_motivo()
    {
        lector.Respuesta = LecturaDeImagen.Fallida(FallaDeLectura.SinPlaca);

        var lectura = await LeerAsync(InicioDePng);

        Assert.False(lectura.Leida);
        Assert.Equal("No se encontró una placa en la imagen", LeerPlacaDesdeImagen.Mensaje(lectura.Falla!.Value));
    }

    [Fact]
    public async Task Un_PDF_se_rechaza_sin_llamar_al_lector()
    {
        var lectura = await LeerAsync(InicioDePdf);

        Assert.Equal(FallaDeLectura.FormatoNoAdmitido, lectura.Falla);
        Assert.Equal("Solo se aceptan imágenes JPG o PNG", LeerPlacaDesdeImagen.Mensaje(lectura.Falla!.Value));
        Assert.Equal(0, lector.Llamadas);
    }

    [Fact]
    public async Task Una_imagen_de_mas_de_5_MB_se_rechaza()
    {
        using var contenido = new MemoryStream(InicioDePng);

        var lectura = await casoDeUso.EjecutarAsync(contenido, LeerPlacaDesdeImagen.TamanoMaximo + 1, TestContext.Current.CancellationToken);

        Assert.Equal(FallaDeLectura.ImagenMuyGrande, lectura.Falla);
        Assert.Equal("La imagen no puede pasar de 5 MB", LeerPlacaDesdeImagen.Mensaje(lectura.Falla!.Value));
        Assert.Equal(0, lector.Llamadas);
    }

    [Fact]
    public async Task Una_imagen_que_dice_pesar_menos_pero_pasa_de_5_MB_tambien_se_rechaza()
    {
        var grande = new byte[LeerPlacaDesdeImagen.TamanoMaximo + 1];
        InicioDePng.CopyTo(grande, 0);
        using var contenido = new MemoryStream(grande);

        var lectura = await casoDeUso.EjecutarAsync(contenido, 1000, TestContext.Current.CancellationToken);

        Assert.Equal(FallaDeLectura.ImagenMuyGrande, lectura.Falla);
        Assert.Equal(0, lector.Llamadas);
    }

    // Con el límite, si el corte dejara de funcionar la prueba falla en vez de quedarse esperando.
    [Fact(Timeout = 10000)]
    public async Task Si_la_lectura_tarda_mas_de_5_segundos_se_corta()
    {
        lector.NoResponde = true;
        using var contenido = new MemoryStream(InicioDePng);

        var lectura = casoDeUso.EjecutarAsync(contenido, InicioDePng.Length, TestContext.Current.CancellationToken);
        reloj.Advance(TimeSpan.FromSeconds(4));
        Assert.False(lectura.IsCompleted);
        reloj.Advance(TimeSpan.FromSeconds(1));

        var resultado = await lectura;
        Assert.Equal(FallaDeLectura.TiempoAgotado, resultado.Falla);
    }

    private async Task<LecturaDeImagen> LeerAsync(byte[] imagen)
    {
        using var contenido = new MemoryStream(imagen);
        return await casoDeUso.EjecutarAsync(contenido, imagen.Length, TestContext.Current.CancellationToken);
    }
}
