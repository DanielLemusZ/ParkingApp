using Microsoft.Extensions.Logging;

namespace ParkingApp.Pruebas.Dobles;

/// <summary>
/// Registro (<see cref="ILogger{TCategoryName}"/>) de prueba: guarda los mensajes para comprobar qué quedó registrado.
/// </summary>
/// <typeparam name="T">Clase que escribe en el registro.</typeparam>
internal sealed class RegistroDePrueba<T> : ILogger<T>
{
    private readonly List<(LogLevel Nivel, string Mensaje, Exception? Error)> entradas = new List<(LogLevel, string, Exception?)>();

    public IReadOnlyList<(LogLevel Nivel, string Mensaje, Exception? Error)> Entradas => entradas;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        entradas.Add((logLevel, formatter(state, exception), exception));
    }
}
