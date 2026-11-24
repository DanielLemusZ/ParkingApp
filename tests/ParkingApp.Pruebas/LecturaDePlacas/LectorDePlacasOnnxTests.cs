using Microsoft.Extensions.Time.Testing;

using ParkingApp.Aplicacion.Accesos;
using ParkingApp.Infraestructura.LecturaDePlacas;
using ParkingApp.Pruebas.Dobles;

using SkiaSharp;

namespace ParkingApp.Pruebas.LecturaDePlacas;

// El lector real, sin cargar los modelos: la salida del OCR y las fallas antes de leer.
public class LectorDePlacasOnnxTests
{
    [Fact]
    public void La_confianza_es_la_menor_probabilidad_en_escala_0_a_100()
    {
        var probabilidades = Salida(("A", 0.99f), ("B", 0.874f), ("C", 1f), ("1", 0.95f), ("2", 0.99f), ("3", 0.99f));

        var (placa, confianza) = TextoDelOcr.Decodificar(probabilidades);

        Assert.Equal("ABC123", placa);
        Assert.Equal(87, confianza);
    }

    [Fact]
    public void El_relleno_del_OCR_no_aparece_en_la_placa()
    {
        // El OCR siempre devuelve 10 posiciones; las que sobran son relleno, aunque tengan probabilidad baja.
        var probabilidades = Salida(("X", 1f), ("Y", 1f), ("Z", 1f), ("1", 1f), ("2", 1f), ("D", 1f), ("_", 0.6f), ("_", 0.9f), ("_", 1f), ("_", 1f));

        var (placa, confianza) = TextoDelOcr.Decodificar(probabilidades);

        Assert.Equal("XYZ12D", placa);
        Assert.Equal(100, confianza);
    }

    [Fact]
    public void Si_solo_hay_relleno_no_hay_placa()
    {
        var (placa, confianza) = TextoDelOcr.Decodificar(Salida(("_", 1f), ("_", 1f)));

        Assert.Equal(string.Empty, placa);
        Assert.Equal(0, confianza);
    }

    [Fact]
    public async Task Una_imagen_danada_se_informa()
    {
        using var lector = new LectorDePlacasOnnx(new RegistroDePrueba<LectorDePlacasOnnx>(), CarpetaSinModelos());
        var danada = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03, 0x04 };

        var lectura = await lector.LeerAsync(danada, TestContext.Current.CancellationToken);

        Assert.Equal(FallaDeLectura.ImagenDanada, lectura.Falla);
    }

    [Fact]
    public async Task Si_faltan_los_modelos_se_pide_escribir_la_placa()
    {
        var registro = new RegistroDePrueba<LectorDePlacasOnnx>();
        using var lector = new LectorDePlacasOnnx(registro, CarpetaSinModelos());
        var casoDeUso = new LeerPlacaDesdeImagen(lector, new FakeTimeProvider());
        using var contenido = new MemoryStream(ImagenEnBlanco());

        var lectura = await casoDeUso.EjecutarAsync(contenido, contenido.Length, TestContext.Current.CancellationToken);

        Assert.Equal(FallaDeLectura.LectorNoDisponible, lectura.Falla);
        Assert.Equal("No se pudo leer la placa: escríbela a mano", LeerPlacaDesdeImagen.Mensaje(lectura.Falla!.Value));
        Assert.Contains(registro.Entradas, entrada => entrada.Mensaje.Contains("Faltan los modelos", StringComparison.Ordinal));
    }

    // Arma la salida del OCR: en cada posición, el carácter indicado con su probabilidad y el resto repartido.
    private static float[] Salida(params (string Caracter, float Probabilidad)[] posiciones)
    {
        var caracteres = TextoDelOcr.Alfabeto.Length;
        var salida = new float[posiciones.Length * caracteres];

        for (var posicion = 0; posicion < posiciones.Length; posicion++)
        {
            var (caracter, probabilidad) = posiciones[posicion];
            var resto = (1f - probabilidad) / (caracteres - 1);

            for (var indice = 0; indice < caracteres; indice++)
            {
                salida[(posicion * caracteres) + indice] = TextoDelOcr.Alfabeto[indice] == caracter[0] ? probabilidad : resto;
            }
        }

        return salida;
    }

    private static string CarpetaSinModelos()
    {
        return Path.Combine(Path.GetTempPath(), "sin-modelos-" + Guid.NewGuid().ToString("N"));
    }

    private static byte[] ImagenEnBlanco()
    {
        using var mapa = new SKBitmap(64, 32);
        mapa.Erase(SKColors.White);
        using var datos = mapa.Encode(SKEncodedImageFormat.Png, 100);
        return datos.ToArray();
    }
}
