using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TutorFlow.Evals.Tests;

public sealed class ServidorHttpFalso : IDisposable
{
    private readonly HttpListener _escucha = new();
    private readonly ConcurrentQueue<string> _cuerpos = new();
    private readonly string _respuestaJson;
    private readonly Task _bucle;

    public ServidorHttpFalso(string respuestaJson)
    {
        _respuestaJson = respuestaJson;
        var puerto = PuertoLibre();
        Url = $"http://127.0.0.1:{puerto}";
        _escucha.Prefixes.Add($"{Url}/");
        _escucha.Start();
        _bucle = Task.Run(AtenderAsync);
    }

    public string Url { get; }

    public IReadOnlyCollection<string> Cuerpos => _cuerpos;

    public void Dispose()
    {
        _escucha.Close();
        _bucle.Wait();
    }

    private static int PuertoLibre()
    {
        var oyente = new TcpListener(IPAddress.Loopback, 0);
        oyente.Start();
        var puerto = ((IPEndPoint)oyente.LocalEndpoint).Port;
        oyente.Stop();
        return puerto;
    }

    private async Task AtenderAsync()
    {
        while (_escucha.IsListening)
        {
            try
            {
                var contexto = await _escucha.GetContextAsync();
                await ResponderAsync(contexto);
            }
            catch (Exception error) when (error is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }
        }
    }

    private async Task ResponderAsync(HttpListenerContext contexto)
    {
        using var lector = new StreamReader(contexto.Request.InputStream, Encoding.UTF8);
        _cuerpos.Enqueue(await lector.ReadToEndAsync());
        var bytes = Encoding.UTF8.GetBytes(_respuestaJson);
        contexto.Response.StatusCode = 200;
        contexto.Response.ContentType = "application/json; charset=utf-8";
        contexto.Response.Headers["X-Correlation-Id"] = Guid.CreateVersion7().ToString();
        contexto.Response.ContentLength64 = bytes.Length;
        await contexto.Response.OutputStream.WriteAsync(bytes);
        contexto.Response.Close();
    }
}
