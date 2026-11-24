using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TutorFlow.Api.Tests;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Evals;

public interface IEjecutor
{
    Task<Ejecucion?> EjecutarAsync(CasoEval caso, CancellationToken cancellationToken);
}

public static class Cuerpos
{
    public const string Ruta = "/api/asistente/preguntar";

    public static string De(CasoEval caso) =>
        JsonSerializer.Serialize(new { pregunta = caso.Pregunta, usuario = new { rol = caso.Usuario.Rol, id = caso.Usuario.Id } });
}

public sealed class EjecutorHttp : IEjecutor
{
    private const string EncabezadoCorrelacion = "X-Correlation-Id";

    private readonly HttpClient _cliente;
    private readonly int _pausaMs;

    public EjecutorHttp(HttpClient cliente, int pausaMs)
    {
        _cliente = cliente;
        _pausaMs = pausaMs;
    }

    public async Task<Ejecucion?> EjecutarAsync(CasoEval caso, CancellationToken cancellationToken)
    {
        if (caso.SoloOffline)
        {
            return null;
        }

        await Task.Delay(_pausaMs, cancellationToken);
        var reloj = Stopwatch.StartNew();
        using var contenido = new StringContent(Cuerpos.De(caso), Encoding.UTF8, "application/json");
        using var respuesta = await _cliente.PostAsync(Cuerpos.Ruta, contenido, cancellationToken);
        var texto = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        reloj.Stop();
        return new Ejecucion((int)respuesta.StatusCode, texto, reloj.Elapsed.TotalMilliseconds, CorrelacionDe(respuesta));
    }

    private static string? CorrelacionDe(HttpResponseMessage respuesta) =>
        respuesta.Headers.TryGetValues(EncabezadoCorrelacion, out var valores) ? valores.FirstOrDefault() : null;
}

public sealed class EjecutorConArtefactos(IEjecutor interno, string carpeta, string? llave) : IEjecutor
{
    private readonly Dictionary<string, int> _corridas = [];

    public async Task<Ejecucion?> EjecutarAsync(CasoEval caso, CancellationToken cancellationToken)
    {
        var ejecucion = await interno.EjecutarAsync(caso, cancellationToken);
        if (ejecucion is not null)
        {
            Guardar(caso, ejecucion);
        }

        return ejecucion;
    }

    private void Guardar(CasoEval caso, Ejecucion ejecucion)
    {
        var corrida = _corridas[caso.Id] = _corridas.GetValueOrDefault(caso.Id) + 1;
        var artefacto = ArtefactosFaseV.Armar(caso, corrida, null, Json.LeerSalida(ejecucion.Cuerpo), ejecucion.CorrelationId);
        ArtefactosFaseV.Guardar(carpeta, $"{TablaFaseV.Pregunta(caso)}-{caso.Id}-corrida-{corrida}", artefacto, llave);
    }
}

public sealed class EjecutorEnProceso : IEjecutor
{
    private const string MarcadorHerramientas = "{herramientas}";

    public async Task<Ejecucion?> EjecutarAsync(CasoEval caso, CancellationToken cancellationToken)
    {
        if (caso.RequiereLlm && caso.Offline is null)
        {
            return null;
        }

        using var fabrica = new FabricaApi(llm: LlmPara(caso.Offline), conocimientoReal: true, configuracion: FabricaApi.Precios("0.10", "0.50"));
        var reloj = Stopwatch.StartNew();
        var respuesta = await fabrica.EnviarAsync(HttpMethod.Post, Cuerpos.Ruta, Cuerpos.De(caso));
        reloj.Stop();
        return new Ejecucion(respuesta.Estado, respuesta.Texto, reloj.Elapsed.TotalMilliseconds);
    }

    internal static ClienteLlmDoble LlmPara(GuionOffline? guion)
    {
        if (guion is null)
        {
            return ClienteLlmDoble.Texto(Textos.SinInformacion);
        }

        return guion.Herramientas == "todas"
            ? ClienteLlmDoble.ConsultaDatos(guion.EstudianteIdCoordinador, solicitud => Plantilla(guion.Texto, solicitud))
            : ClienteLlmDoble.Texto(guion.Texto ?? Textos.SinInformacion);
    }

    private static string Plantilla(string? texto, SolicitudLlm solicitud) =>
        (texto ?? MarcadorHerramientas).Replace(MarcadorHerramientas, ClienteLlmDoble.EcoDeHerramientas(solicitud), StringComparison.Ordinal);
}
