using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class ClienteLlmLento : IClienteLlm
{
    private readonly RelojControlado _reloj;
    private readonly TimeSpan _demora;
    private readonly int _llamadasConHerramienta;
    private int _llamadas;

    public ClienteLlmLento(RelojControlado reloj, TimeSpan demora, int llamadasConHerramienta)
    {
        _reloj = reloj;
        _demora = demora;
        _llamadasConHerramienta = llamadasConHerramienta;
    }

    public int Llamadas => Volatile.Read(ref _llamadas);

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        var numero = Interlocked.Increment(ref _llamadas) - 1;
        await _reloj.Pausar(_demora, cancellationToken);
        return numero < _llamadasConHerramienta
            ? new RespuestaLlm(null, [new LlamadaHerramienta($"lenta-{numero}", solicitud.Herramientas[0].Nombre, "{}")], ConsumoRealista.UsoDe(solicitud, ClienteLlmDoble.UsoPorDefecto.Salida), false)
            : new RespuestaLlm("Respuesta de prueba.", [], ConsumoRealista.UsoDe(solicitud, ClienteLlmDoble.UsoPorDefecto.Salida), false);
    }
}

public sealed class ClienteLlmRetenido : IClienteLlm
{
    private readonly TaskCompletionSource _liberacion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly UsoTokens _uso;
    private int _llamadas;

    public ClienteLlmRetenido(UsoTokens uso)
    {
        _uso = uso;
    }

    public int Llamadas => Volatile.Read(ref _llamadas);

    public void Liberar() => _liberacion.TrySetResult();

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _llamadas);
        await _liberacion.Task.WaitAsync(cancellationToken);
        return new RespuestaLlm("Respuesta de prueba.", [], ConsumoRealista.UsoDe(solicitud, _uso.Salida), false);
    }
}

public static partial class GuionesLlm
{
    public static ClienteLlmDoble SinHerramientas(Func<SolicitudLlm, string> redactar) =>
        new((solicitud, _) => new RespuestaLlm(redactar(solicitud), [], ConsumoRealista.UsoDe(solicitud, ClienteLlmDoble.UsoPorDefecto.Salida), false));

    public static IReadOnlyList<BloqueDelimitado> Bloques(SolicitudLlm solicitud) =>
        BloqueRegex().Matches(string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido)))
            .Select(m => new BloqueDelimitado(m.Groups["fuente"].Value, m.Groups["cuerpo"].Value))
            .ToList();

    public static BloqueDelimitado? BloqueDeResumen(SolicitudLlm solicitud, string asignatura) =>
        Bloques(solicitud).FirstOrDefault(b => b.Cuerpo.Contains(asignatura, StringComparison.Ordinal));

    public static string ResumenOSinDatos(SolicitudLlm solicitud, string asignatura) =>
        BloqueDeResumen(solicitud, asignatura)?.Cuerpo ?? "Sin datos";

    public static string LineaDe(SolicitudLlm solicitud, string asignatura) =>
        BloqueDeResumen(solicitud, asignatura)?.Cuerpo
            .Split('\n')
            .FirstOrDefault(l => l.Contains(asignatura, StringComparison.Ordinal))
        ?? "Sin datos";

    public static IReadOnlyList<string> PalabrasDelPrompt(SolicitudLlm solicitud, int omitir, int cantidad) =>
        solicitud.PromptSistema.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Skip(omitir).Take(cantidad).ToList();

    [GeneratedRegex("<<<DATOS_NO_CONFIABLES fuente=\"(?<fuente>[^\"]*)\">>>\\n(?<cuerpo>.*?)\\nFIN_DATOS_NO_CONFIABLES>>>", RegexOptions.Singleline)]
    private static partial Regex BloqueRegex();
}

public sealed record BloqueDelimitado(string Fuente, string Cuerpo);

public sealed class ServidorLocal : IAsyncDisposable
{
    private readonly WebApplication _aplicacion;
    private int _peticiones;
    private readonly List<string> _autorizaciones = [];

    private ServidorLocal(WebApplication aplicacion)
    {
        _aplicacion = aplicacion;
    }

    public string Url => _aplicacion.Urls.First();

    public int Peticiones => Volatile.Read(ref _peticiones);

    public IReadOnlyList<string> Autorizaciones
    {
        get
        {
            lock (_autorizaciones)
            {
                return [.. _autorizaciones];
            }
        }
    }

    public static async Task<ServidorLocal> IniciarAsync(Func<HttpContext, Task> manejador)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var aplicacion = builder.Build();
        ServidorLocal? servidor = null;
        aplicacion.Run(contexto =>
        {
            servidor!.Registrar(contexto);
            return manejador(contexto);
        });
        servidor = new ServidorLocal(aplicacion);
        await aplicacion.StartAsync();
        return servidor;
    }

    public async ValueTask DisposeAsync()
    {
        await _aplicacion.StopAsync();
        await _aplicacion.DisposeAsync();
    }

    private void Registrar(HttpContext contexto)
    {
        Interlocked.Increment(ref _peticiones);
        lock (_autorizaciones)
        {
            _autorizaciones.Add(contexto.Request.Headers.Authorization.ToString() + contexto.Request.Headers["x-api-key"]);
        }
    }
}
