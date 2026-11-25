using System.Net;
using System.Text;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Fábrica de clientes HTTP de prueba: cada solicitud la responde una función, sin salir a internet.
/// </summary>
internal sealed class FabricaDeClientesDePrueba : IHttpClientFactory
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
    private readonly List<Uri> solicitudes = new List<Uri>();

    public FabricaDeClientesDePrueba(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        this.responder = responder;
    }

    public IReadOnlyList<Uri> Solicitudes => solicitudes;

    public static HttpResponseMessage Json(string json, HttpStatusCode estado = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new Manejador(responder, solicitudes)) { BaseAddress = new Uri("https://pasarela.test/") };
    }

    private sealed class Manejador : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;
        private readonly List<Uri> solicitudes;

        public Manejador(Func<HttpRequestMessage, HttpResponseMessage> responder, List<Uri> solicitudes)
        {
            this.responder = responder;
            this.solicitudes = solicitudes;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            solicitudes.Add(request.RequestUri!);
            return Task.FromResult(responder(request));
        }
    }
}
