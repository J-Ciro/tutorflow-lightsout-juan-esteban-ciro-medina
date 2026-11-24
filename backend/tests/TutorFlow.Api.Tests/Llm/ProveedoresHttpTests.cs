using TutorFlow.Pruebas.Soporte;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Llm;

public sealed record PeticionLlm(Uri Uri, IReadOnlyDictionary<string, string> Encabezados, string Cuerpo);

public sealed class ManejadorLlmFalso : HttpMessageHandler
{
    private readonly ConcurrentQueue<PeticionLlm> _peticiones = new();
    private readonly Func<PeticionLlm, int, HttpResponseMessage> _guion;

    public ManejadorLlmFalso(Func<PeticionLlm, int, HttpResponseMessage> guion)
    {
        _guion = guion;
    }

    public IReadOnlyList<PeticionLlm> Peticiones => [.. _peticiones];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var encabezados = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var peticion = new PeticionLlm(request.RequestUri!, encabezados, cuerpo);
        int numero;
        lock (_peticiones)
        {
            numero = _peticiones.Count;
            _peticiones.Enqueue(peticion);
        }

        return _guion(peticion, numero);
    }
}

public static class RespuestasProveedor
{
    public static bool EsAnthropic(PeticionLlm peticion) => peticion.Uri.Host.Contains("anthropic", StringComparison.OrdinalIgnoreCase);

    public static HttpResponseMessage Json(string contenido, HttpStatusCode estado = HttpStatusCode.OK) =>
        new(estado) { Content = new StringContent(contenido, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Texto(PeticionLlm peticion, string texto, int salida) =>
        Texto(peticion, texto, ConsumoRealistaHttp.Reportados(peticion), salida);

    public static HttpResponseMessage LlamadaAHerramienta(PeticionLlm peticion, int salida) =>
        LlamadaAHerramienta(peticion, ConsumoRealistaHttp.Reportados(peticion), salida);

    public static HttpResponseMessage Texto(PeticionLlm peticion, string texto, int entrada, int salida) =>
        Serializar(EsAnthropic(peticion)
            ? new
            {
                id = "msg_1",
                type = "message",
                role = "assistant",
                model = "m",
                content = new object[] { new { type = "text", text = texto } },
                stop_reason = "end_turn",
                usage = new { input_tokens = entrada, output_tokens = salida }
            }
            : new
            {
                id = "c1",
                @object = "chat.completion",
                choices = new object[] { new { index = 0, message = new { role = "assistant", content = texto, refusal = (string?)null }, finish_reason = "stop" } },
                usage = new { prompt_tokens = entrada, completion_tokens = salida, total_tokens = entrada + salida }
            });

    public static HttpResponseMessage LlamadaAHerramienta(PeticionLlm peticion, int entrada, int salida)
    {
        using var cuerpo = JsonDocument.Parse(peticion.Cuerpo);
        var primera = cuerpo.RootElement.GetProperty("tools")[0];
        return EsAnthropic(peticion)
            ? LlamadaAnthropic(primera.GetProperty("name").GetString(), entrada, salida)
            : LlamadaOpenAi(primera.GetProperty("function").GetProperty("name").GetString(), entrada, salida);
    }

    private static HttpResponseMessage LlamadaAnthropic(string? herramienta, int entrada, int salida) =>
        Serializar(new
        {
            id = "msg_2",
            type = "message",
            role = "assistant",
            model = "m",
            content = new object[] { new { type = "tool_use", id = "tu_1", name = herramienta, input = new { } } },
            stop_reason = "tool_use",
            usage = new { input_tokens = entrada, output_tokens = salida }
        });

    private static HttpResponseMessage LlamadaOpenAi(string? herramienta, int entrada, int salida) =>
        Serializar(new
        {
            id = "c2",
            @object = "chat.completion",
            choices = new object[] { new { index = 0, message = MensajeConHerramienta(herramienta), finish_reason = "tool_calls" } },
            usage = new { prompt_tokens = entrada, completion_tokens = salida, total_tokens = entrada + salida }
        });

    private static object MensajeConHerramienta(string? herramienta) =>
        new
        {
            role = "assistant",
            content = (string?)null,
            tool_calls = new object[] { new { id = "call_1", type = "function", function = new { name = herramienta, arguments = "{}" } } }
        };

    private static HttpResponseMessage Serializar(object valor) => Json(JsonSerializer.Serialize(valor));

    public static HttpResponseMessage Rechazo(PeticionLlm peticion)
    {
        var entrada = ConsumoRealistaHttp.Reportados(peticion);
        return Json(EsAnthropic(peticion)
            ? $$$"""{"id":"msg_3","type":"message","role":"assistant","model":"m","content":[],"stop_reason":"refusal","usage":{"input_tokens":{{{entrada}}},"output_tokens":0}}"""
            : $$$"""{"id":"c3","object":"chat.completion","choices":[{"index":0,"message":{"role":"assistant","content":null,"refusal":"No puedo ayudar con eso"},"finish_reason":"stop"}],"usage":{"prompt_tokens":{{{entrada}}},"completion_tokens":0,"total_tokens":{{{entrada}}}}}""");
    }
}

public sealed class ProveedoresHttpTests
{
    private const string Normativa = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string RespuestaNormativa = "Una asignatura se aprueba con 3.0.";
    private const int SalidaPorLlamada = 200;
    private const decimal PrecioEntrada = 1.0m;
    private const decimal PrecioSalida = 5.0m;
    private const decimal ToleranciaDeCosto = 0.000001m;

    private static void AfirmarConsumo(RespuestaApi respuesta, ManejadorLlmFalso manejador)
    {
        var entrada = manejador.Peticiones.Sum(ConsumoRealistaHttp.Reportados);
        var salida = manejador.Peticiones.Count * SalidaPorLlamada;
        Assert.Equal(entrada + salida, respuesta.Tokens);
        Assert.InRange(respuesta.CostoUsd, ConsumoRealista.CostoDe(entrada, salida, PrecioEntrada, PrecioSalida) - ToleranciaDeCosto, ConsumoRealista.CostoDe(entrada, salida, PrecioEntrada, PrecioSalida) + ToleranciaDeCosto);
    }

    public static TheoryData<string?, string, string> Proveedores => new()
    {
        { "sk-ant-LLAVE-ABC123", "api.anthropic.com", "claude-haiku-5-5" },
        { "sk-LLAVE-ABC123-openai", "api.openai.com", "gpt-4o-mini" },
        { "gsk_LLAVEABC123", "api.groq.com", "llama-3.1-8b-instant" },
        { "sk-or-LLAVE-ABC123", "openrouter.ai", "openai/gpt-4o-mini" },
        { "AIzaLLAVEABC123", "generativelanguage.googleapis.com", "gemini-2.0-flash" },
        { null, "host.docker.internal", "qwen2.5:7b" }
    };

    public static TheoryData<string?> Llaves => new() { "sk-ant-LLAVE-ABC123", "sk-LLAVE-ABC123-openai", "gsk_LLAVEABC123", "sk-or-LLAVE-ABC123", "AIzaLLAVEABC123", null };

    private static Dictionary<string, string?> Llave(string? llave) =>
        llave is null ? [] : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave };

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador) =>
        new(configuracion: FabricaApi.Precios("1.0", "5.0"), variablesEntorno: Llave(llave), reemplazarLlm: false, manejadorHttp: manejador);

    private static string? EncabezadoDeAutenticacion(PeticionLlm peticion) =>
        peticion.Encabezados.TryGetValue("x-api-key", out var anthropic)
            ? anthropic
            : peticion.Encabezados.GetValueOrDefault("Authorization")?.Replace("Bearer ", string.Empty, StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    public async Task Respuesta_simple_reporta_tokens_costo_modelo_y_llave_solo_hacia_el_proveedor(string? llave, string host, string modelo)
    {
        var manejador = new ManejadorLlmFalso((p, _) => RespuestasProveedor.Texto(p, RespuestaNormativa, SalidaPorLlamada));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(RespuestaNormativa, respuesta.Respuesta);
        AfirmarConsumo(respuesta, manejador);
        Assert.NotEmpty(manejador.Peticiones);
        Assert.All(manejador.Peticiones, p => Assert.Equal(host, p.Uri.Host));
        Assert.All(manejador.Peticiones, p => Assert.Contains(modelo, p.Cuerpo, StringComparison.Ordinal));
        Assert.All(manejador.Peticiones, p => Assert.Contains("2000", p.Cuerpo, StringComparison.Ordinal));
        AfirmarLlaveSoloEnEncabezado(llave, manejador, respuesta, fabrica);
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    public async Task Flujo_con_herramienta_suma_tokens_de_ambas_invocaciones_y_entrega_el_resultado_al_modelo(string? llave, string host, string modelo)
    {
        var manejador = new ManejadorLlmFalso((p, n) => n == 0
            ? RespuestasProveedor.LlamadaAHerramienta(p, SalidaPorLlamada)
            : RespuestasProveedor.Texto(p, "Datos revisados.", SalidaPorLlamada));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio ponderado del periodo?");

        Assert.Equal(200, respuesta.Estado);
        Assert.True(respuesta.UsoDatosEstudiante);
        AfirmarConsumo(respuesta, manejador);
        Assert.Equal(2, manejador.Peticiones.Count);
        Assert.Contains("4.39", manejador.Peticiones[1].Cuerpo, StringComparison.Ordinal);
        Assert.All(manejador.Peticiones, p => Assert.Equal(host, p.Uri.Host));
        Assert.All(manejador.Peticiones, p => Assert.Contains(modelo, p.Cuerpo, StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Llaves))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    public async Task Rechazo_del_modelo_se_responde_con_texto_no_verificable_y_auditoria_error(string? llave)
    {
        var manejador = new ManejadorLlmFalso((p, _) => RespuestasProveedor.Rechazo(p));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-109")]
    public async Task Error_5xx_del_proveedor_responde_503_sin_llave_url_ni_modelo(string? llave, string host, string modelo)
    {
        var manejador = new ManejadorLlmFalso((_, _) => RespuestasProveedor.Json("""{"error":"fallo interno del proveedor"}""", HttpStatusCode.InternalServerError));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.DoesNotContain(host, respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(modelo, respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fallo interno", respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        if (llave is not null)
        {
            Assert.DoesNotContain(llave, respuesta.Texto, StringComparison.Ordinal);
            Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(llave, StringComparison.Ordinal));
        }
    }

    [Theory]
    [MemberData(nameof(Llaves))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-109")]
    public async Task Timeout_del_proveedor_responde_503(string? llave)
    {
        var manejador = new ManejadorLlmFalso((_, _) => throw new TaskCanceledException("tiempo agotado"));
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Afirmaciones.ProblemDetails(respuesta, 503);
    }

    [Theory]
    [MemberData(nameof(Llaves))]
    [Trait("CA", "CA-04")]
    [Trait("ESC", "ESC-079")]
    public async Task Las_herramientas_enviadas_al_proveedor_no_tienen_parametros_de_id_para_el_estudiante(string? llave)
    {
        var manejador = new ManejadorLlmFalso((p, _) => RespuestasProveedor.Texto(p, "Listo.", 5));
        using var fabrica = Fabrica(llave, manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        var cuerpo = manejador.Peticiones[0].Cuerpo;
        Assert.DoesNotContain("estudianteId", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    private static void AfirmarLlaveSoloEnEncabezado(string? llave, ManejadorLlmFalso manejador, RespuestaApi respuesta, FabricaApi fabrica)
    {
        if (llave is null)
        {
            return;
        }

        Assert.All(manejador.Peticiones, p => Assert.Equal(llave, EncabezadoDeAutenticacion(p)));
        Assert.All(manejador.Peticiones, p => Assert.DoesNotContain(llave, p.Cuerpo, StringComparison.Ordinal));
        Assert.DoesNotContain(llave, respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(llave, StringComparison.Ordinal));
    }
}
