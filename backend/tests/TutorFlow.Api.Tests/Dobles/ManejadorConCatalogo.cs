using System.Collections.Concurrent;

namespace TutorFlow.Api.Tests.Dobles;

public sealed record SolicitudCatalogo(Uri Uri, IReadOnlyDictionary<string, string> Encabezados, string Cuerpo);

public sealed class ManejadorConCatalogo : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<HttpResponseMessage>> _catalogo;
    private readonly int? _entrada;
    private readonly int _salida;
    private readonly ConcurrentQueue<SolicitudCatalogo> _solicitudesCatalogo = new();
    private readonly ConcurrentQueue<PeticionLlm> _peticionesLlm = new();
    private int _catalogoAtendido;

    public ManejadorConCatalogo(Func<CancellationToken, Task<HttpResponseMessage>> catalogo, int? entrada, int salida)
    {
        _catalogo = catalogo;
        _entrada = entrada;
        _salida = salida;
    }

    public IReadOnlyList<SolicitudCatalogo> SolicitudesCatalogo => [.. _solicitudesCatalogo];

    public IReadOnlyList<PeticionLlm> PeticionesLlm => [.. _peticionesLlm];

    public int CatalogoAtendido => Volatile.Read(ref _catalogoAtendido);

    public static HttpResponseMessage Catalogo(string json) => RespuestasProveedor.Json(json);

    public static string JsonDeCatalogo(string id, string prompt, string completion) =>
        $$$"""{"data":[{"id":"otro/modelo","pricing":{"prompt":"0.1","completion":"0.1"}},{"id":"{{{id}}}","pricing":{"prompt":"{{{prompt}}}","completion":"{{{completion}}}"}}]}""";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var encabezados = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.EndsWith("/models", StringComparison.Ordinal))
        {
            _solicitudesCatalogo.Enqueue(new SolicitudCatalogo(request.RequestUri, encabezados, cuerpo));
            try
            {
                return await _catalogo(cancellationToken);
            }
            finally
            {
                Interlocked.Increment(ref _catalogoAtendido);
            }
        }

        var peticion = new PeticionLlm(request.RequestUri!, encabezados, cuerpo);
        _peticionesLlm.Enqueue(peticion);
        return _entrada is int entrada
            ? RespuestasProveedor.Texto(peticion, "Respuesta de prueba.", entrada, _salida)
            : RespuestasProveedor.Texto(peticion, "Respuesta de prueba.", _salida);
    }
}
