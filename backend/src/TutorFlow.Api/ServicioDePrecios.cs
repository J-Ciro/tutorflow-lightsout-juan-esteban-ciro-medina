using TutorFlow.Core.Asistente;
using TutorFlow.Infrastructure.Llm;

namespace TutorFlow.Api;

public sealed class ServicioDePrecios : BackgroundService
{
    public const string NombreCliente = "catalogo-precios";
    private const string UrlPorDefecto = "https://openrouter.ai/api/v1/models";
    private const int SegundosPorIntento = 5;
    private const int Intentos = 2;
    private const int BytesPorLectura = 81_920;

    private static readonly TimeSpan TiempoPorIntento = TimeSpan.FromSeconds(SegundosPorIntento);

    private readonly IHttpClientFactory _fabricaHttp;
    private readonly ProveedorResuelto _proveedor;
    private readonly string? _url;
    private readonly ILogger _logger;
    private volatile CatalogoDePrecios? _catalogo;

    public ServicioDePrecios(IHttpClientFactory fabricaHttp, ProveedorResuelto proveedor, IConfiguration configuracion, ILoggerFactory fabricaDeLogs)
    {
        _fabricaHttp = fabricaHttp;
        _proveedor = proveedor;
        _url = configuracion[ReglaDeConfiguracion.ClaveCatalogoPrecios] ?? UrlPorDefecto;
        _logger = fabricaDeLogs.CreateLogger("TutorFlow.Configuracion");
    }

    private bool SinProveedor => _proveedor.Adaptador == AdaptadorLlm.NoConfigurado;

    private bool EsOllama => _proveedor.Proveedor == ProveedorLlm.Ollama;

    public PrecioModelo? Vigente()
    {
        if (EsOllama)
        {
            return new PrecioModelo(0m, 0m);
        }

        return SinProveedor ? null : _catalogo?.Buscar(_proveedor.Proveedor, _proveedor.Modelo) ?? DeLaTabla();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        if (EsOllama || SinProveedor)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_url))
        {
            _catalogo = await DescargarAsync(_url, stoppingToken);
        }

        AdvertirSiFalta();
    }

    private PrecioModelo? DeLaTabla() =>
        _proveedor.PrecioDesconocido ? null : new PrecioModelo(_proveedor.PrecioEntradaPorMillon, _proveedor.PrecioSalidaPorMillon);

    private void AdvertirSiFalta()
    {
        if (Vigente() is null)
        {
            var nombre = ValorDeMensaje.De(_proveedor.Modelo);
            ValidacionDeArranque.RegistrarAdvertencia(_logger, $"{ValidadorDeConfiguracion.PrefijoIgnorada}: no se encontró el precio del modelo {nombre} en el catálogo público ni en appsettings; costoUsd será 0.");
        }
    }

    private async Task<CatalogoDePrecios?> DescargarAsync(string url, CancellationToken detener)
    {
        for (var intento = 0; intento < Intentos; intento++)
        {
            try
            {
                return await IntentarAsync(url, detener);
            }
            catch (Exception) when (!detener.IsCancellationRequested)
            {
            }
        }

        return null;
    }

    private async Task<CatalogoDePrecios> IntentarAsync(string url, CancellationToken detener)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(detener);
        limite.CancelAfter(TiempoPorIntento);
        using var respuesta = await _fabricaHttp.CreateClient(NombreCliente).GetAsync(url, HttpCompletionOption.ResponseHeadersRead, limite.Token);
        respuesta.EnsureSuccessStatusCode();
        return CatalogoDePrecios.Leer(await LeerAcotadoAsync(respuesta, limite.Token));
    }

    private static async Task<byte[]> LeerAcotadoAsync(HttpResponseMessage respuesta, CancellationToken detener)
    {
        await using var flujo = await respuesta.Content.ReadAsStreamAsync(detener);
        using var memoria = new MemoryStream();
        var buffer = new byte[BytesPorLectura];
        int leidos;
        while ((leidos = await flujo.ReadAsync(buffer, detener)) > 0)
        {
            memoria.Write(buffer, 0, leidos);
            if (memoria.Length > CatalogoDePrecios.BytesMaximos)
            {
                throw new InvalidDataException("El catálogo excede el tamaño máximo.");
            }
        }

        return memoria.ToArray();
    }
}
