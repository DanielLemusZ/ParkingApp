using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using ParkingApp.Aplicacion.Accesos;

using SkiaSharp;

namespace ParkingApp.Infraestructura.LecturaDePlacas;

/// <summary>
/// Lector de placas con los modelos de fast-alpr, en el mismo equipo y sin internet (patrón Adapter): un detector
/// ubica la placa en la foto y un OCR la lee. Los mismos modelos leen placas de carro y de moto.
/// </summary>
public sealed class LectorDePlacasOnnx : ILectorDePlacas, IDisposable
{
    /// <summary>
    /// Archivo del detector de placas (open-image-models).
    /// </summary>
    public const string ArchivoDetector = "yolo-v9-t-384-license-plates-end2end.onnx";

    /// <summary>
    /// Archivo del OCR de placas (fast-plate-ocr).
    /// </summary>
    public const string ArchivoOcr = "cct_xs_v2_global.onnx";

    // El detector recibe una imagen cuadrada de 384 píxeles; el OCR, un recorte de 128 x 64.
    private const int LadoDelDetector = 384;
    private const int AnchoDelOcr = 128;
    private const int AltoDelOcr = 64;

    // Puntaje mínimo de una detección. Es el que usa fast-alpr por defecto.
    private const float PuntajeMinimo = 0.4f;

    // Las fotos más grandes se reducen al abrirlas: sobra resolución para leer la placa y se ahorra memoria.
    private const int LadoMaximo = 2000;

    private static readonly SKSamplingOptions Lineal = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

    private readonly ILogger<LectorDePlacasOnnx> registro;
    private readonly string carpeta;
    private readonly object candado = new object();
    private InferenceSession? detector;
    private InferenceSession? ocr;

    /// <summary>
    /// Crea el lector con los modelos de la carpeta <c>Modelos</c>, junto a la aplicación.
    /// </summary>
    /// <param name="registro">Registro donde quedan las fallas del lector.</param>
    public LectorDePlacasOnnx(ILogger<LectorDePlacasOnnx> registro)
        : this(registro, Path.Combine(AppContext.BaseDirectory, "Modelos"))
    {
    }

    /// <summary>
    /// Crea el lector con los modelos de otra carpeta.
    /// </summary>
    /// <param name="registro">Registro donde quedan las fallas del lector.</param>
    /// <param name="carpeta">Carpeta con los dos archivos de modelos.</param>
    public LectorDePlacasOnnx(ILogger<LectorDePlacasOnnx> registro, string carpeta)
    {
        this.registro = registro;
        this.carpeta = carpeta;
    }

    /// <inheritdoc />
    public Task<LecturaDeImagen> LeerAsync(byte[] imagen, CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(imagen);

        // Leer es trabajo de procesador: se hace aparte para no detener la pantalla mientras tanto.
        return Task.Run(() => Leer(imagen), cancelacion);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        detector?.Dispose();
        ocr?.Dispose();
    }

    private LecturaDeImagen Leer(byte[] imagen)
    {
        using var foto = Abrir(imagen);

        if (foto is null)
        {
            return LecturaDeImagen.Fallida(FallaDeLectura.ImagenDanada);
        }

        try
        {
            if (!CargarModelos())
            {
                return LecturaDeImagen.Fallida(FallaDeLectura.LectorNoDisponible);
            }

            using var recorte = RecortarPlaca(foto);

            if (recorte is null)
            {
                return LecturaDeImagen.Fallida(FallaDeLectura.SinPlaca);
            }

            var (placa, confianza) = LeerRecorte(recorte);

            if (placa.Length == 0)
            {
                return LecturaDeImagen.Fallida(FallaDeLectura.SinPlaca);
            }

            return LecturaDeImagen.Exitosa(placa, confianza);
        }
        catch (Exception error) when (error is OnnxRuntimeException or InvalidOperationException)
        {
            // Un modelo dañado o incompatible: el carril sigue funcionando con la placa escrita a mano.
            registro.LogError(error, "El lector de placas no pudo usar los modelos de {Carpeta}", carpeta);
            return LecturaDeImagen.Fallida(FallaDeLectura.LectorNoDisponible);
        }
    }

    // Carga los modelos la primera vez. Si faltan, avisa y lo vuelve a intentar en la siguiente lectura.
    private bool CargarModelos()
    {
        lock (candado)
        {
            if (detector is not null && ocr is not null)
            {
                return true;
            }

            var rutaDetector = Path.Combine(carpeta, ArchivoDetector);
            var rutaOcr = Path.Combine(carpeta, ArchivoOcr);

            if (!File.Exists(rutaDetector) || !File.Exists(rutaOcr))
            {
                registro.LogWarning("Faltan los modelos del lector de placas en {Carpeta}: la placa se escribe a mano", carpeta);
                return false;
            }

            // En Windows, ONNX Runtime envía eventos de uso por la telemetría del sistema. Se apaga antes de crear los modelos.
            OrtEnv.Instance().DisableTelemetryEvents();
            detector ??= new InferenceSession(rutaDetector);
            ocr ??= new InferenceSession(rutaOcr);
            return true;
        }
    }

    // Abre la imagen, la reduce si es muy grande y la endereza si el celular la guardó de lado. Null si no se puede abrir.
    private static SKBitmap? Abrir(byte[] imagen)
    {
        using var datos = SKData.CreateCopy(imagen);
        using var codec = SKCodec.Create(datos);

        if (codec is null)
        {
            return null;
        }

        var ladoMayor = Math.Max(codec.Info.Width, codec.Info.Height);
        var escala = ladoMayor > LadoMaximo ? (float)LadoMaximo / ladoMayor : 1f;
        var tamano = codec.GetScaledDimensions(escala);
        var info = new SKImageInfo(tamano.Width, tamano.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var mapa = new SKBitmap(info);
        var resultado = codec.GetPixels(info, mapa.GetPixels());

        if (resultado is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
        {
            mapa.Dispose();
            return null;
        }

        return Enderezar(mapa, codec.EncodedOrigin);
    }

    private static SKBitmap Enderezar(SKBitmap mapa, SKEncodedOrigin origen)
    {
        var grados = origen switch
        {
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.LeftBottom => 270,
            _ => 0,
        };

        if (grados == 0)
        {
            return mapa;
        }

        var girada = grados == 180 ? new SKBitmap(mapa.Width, mapa.Height) : new SKBitmap(mapa.Height, mapa.Width);

        using (var lienzo = new SKCanvas(girada))
        using (var imagen = SKImage.FromBitmap(mapa))
        {
            lienzo.Translate(girada.Width / 2f, girada.Height / 2f);
            lienzo.RotateDegrees(grados);
            lienzo.Translate(-mapa.Width / 2f, -mapa.Height / 2f);
            lienzo.DrawImage(imagen, 0, 0, Lineal);
        }

        mapa.Dispose();
        return girada;
    }

    // Busca la placa con el detector y devuelve su recorte, o null si no hay ninguna con puntaje suficiente.
    private SKBitmap? RecortarPlaca(SKBitmap foto)
    {
        // La foto se reduce sin deformarla y se completa con gris hasta formar el cuadrado que espera el detector.
        var escala = Math.Min((float)LadoDelDetector / foto.Width, (float)LadoDelDetector / foto.Height);
        var ancho = (int)Math.Round(foto.Width * escala);
        var alto = (int)Math.Round(foto.Height * escala);
        var margenX = (LadoDelDetector - ancho) / 2f;
        var margenY = (LadoDelDetector - alto) / 2f;
        var izquierda = (int)Math.Round(margenX - 0.1);
        var arriba = (int)Math.Round(margenY - 0.1);

        using var reducida = Escalar(foto, ancho, alto);
        var pixeles = reducida.GetPixelSpan();
        var area = LadoDelDetector * LadoDelDetector;
        var entrada = new float[3 * area];

        for (var y = 0; y < LadoDelDetector; y++)
        {
            for (var x = 0; x < LadoDelDetector; x++)
            {
                var origenX = x - izquierda;
                var origenY = y - arriba;
                var dentro = origenX >= 0 && origenY >= 0 && origenX < ancho && origenY < alto;
                var posicion = (origenY * reducida.RowBytes) + (origenX * 4);

                for (var canal = 0; canal < 3; canal++)
                {
                    var valor = dentro ? pixeles[posicion + canal] : (byte)114;
                    entrada[(canal * area) + (y * LadoDelDetector) + x] = valor / 255f;
                }
            }
        }

        var tensor = new DenseTensor<float>(entrada, new[] { 1, 3, LadoDelDetector, LadoDelDetector });
        using var salida = detector!.Run(new[] { NamedOnnxValue.CreateFromTensor(detector.InputMetadata.Keys.First(), tensor) });

        // Cada detección son 7 números: lote, x1, y1, x2, y2, clase y puntaje. Se toma la de mayor puntaje.
        var detecciones = salida[0].AsTensor<float>().ToArray();
        var mejor = -1;

        for (var inicio = 0; inicio + 6 < detecciones.Length; inicio += 7)
        {
            var puntaje = detecciones[inicio + 6];

            if (puntaje >= PuntajeMinimo && (mejor < 0 || puntaje > detecciones[mejor + 6]))
            {
                mejor = inicio;
            }
        }

        if (mejor < 0)
        {
            return null;
        }

        var x1 = Math.Max(0, (int)((detecciones[mejor + 1] - margenX) / escala));
        var y1 = Math.Max(0, (int)((detecciones[mejor + 2] - margenY) / escala));
        var x2 = Math.Min(foto.Width, (int)((detecciones[mejor + 3] - margenX) / escala));
        var y2 = Math.Min(foto.Height, (int)((detecciones[mejor + 4] - margenY) / escala));

        if (x2 <= x1 || y2 <= y1)
        {
            return null;
        }

        using var parte = new SKBitmap();
        return foto.ExtractSubset(parte, new SKRectI(x1, y1, x2, y2)) ? parte.Copy(SKColorType.Rgba8888) : null;
    }

    private (string Placa, int Confianza) LeerRecorte(SKBitmap recorte)
    {
        using var reducido = Escalar(recorte, AnchoDelOcr, AltoDelOcr);
        var pixeles = reducido.GetPixelSpan();
        var entrada = new byte[AltoDelOcr * AnchoDelOcr * 3];

        for (var y = 0; y < AltoDelOcr; y++)
        {
            for (var x = 0; x < AnchoDelOcr; x++)
            {
                for (var canal = 0; canal < 3; canal++)
                {
                    entrada[(((y * AnchoDelOcr) + x) * 3) + canal] = pixeles[(y * reducido.RowBytes) + (x * 4) + canal];
                }
            }
        }

        var tensor = new DenseTensor<byte>(entrada, new[] { 1, AltoDelOcr, AnchoDelOcr, 3 });
        using var salida = ocr!.Run(new[] { NamedOnnxValue.CreateFromTensor(ocr.InputMetadata.Keys.First(), tensor) });

        // El OCR también devuelve el país de la placa ("region"); solo se usa el texto ("plate").
        DisposableNamedOnnxValue? textoDeLaPlaca = null;

        foreach (var valor in salida)
        {
            if (valor.Name == "plate")
            {
                textoDeLaPlaca = valor;
            }
        }

        if (textoDeLaPlaca is null)
        {
            throw new InvalidOperationException("El modelo del OCR no tiene la salida \"plate\".");
        }

        return TextoDelOcr.Decodificar(textoDeLaPlaca.AsTensor<float>().ToArray());
    }

    private static SKBitmap Escalar(SKBitmap origen, int ancho, int alto)
    {
        var destino = new SKBitmap(new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul));
        origen.ScalePixels(destino, Lineal);
        return destino;
    }
}
