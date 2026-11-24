using TutorFlow.Pruebas.Soporte;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Dobles;

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

