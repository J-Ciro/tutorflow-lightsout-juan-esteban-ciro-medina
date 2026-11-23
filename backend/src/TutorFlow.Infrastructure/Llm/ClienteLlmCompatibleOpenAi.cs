using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed class ClienteLlmCompatibleOpenAi : IClienteLlm
{
    private const string RutaChatCompletions = "chat/completions";
    private const string RutaConVersion = "v1/chat/completions";
    private const string FinPorContenidoFiltrado = "content_filter";

    private readonly HttpClient _httpClient;
    private readonly ProveedorResuelto _proveedor;
    private readonly string? _llave;

    public ClienteLlmCompatibleOpenAi(HttpClient httpClient, ProveedorResuelto proveedor, string? llave)
    {
        _httpClient = httpClient;
        _proveedor = proveedor;
        _llave = llave;
    }

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Post, ConstruirUri())
        {
            Content = JsonContent.Create(ConstruirCuerpo(solicitud))
        };
        if (!string.IsNullOrWhiteSpace(_llave))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _llave);
        }

        using var respuesta = await _httpClient.SendAsync(peticion, cancellationToken);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new DependenciaNoDisponibleException("El proveedor del LLM respondió con un error.");
        }

        var contenido = await respuesta.Content.ReadFromJsonAsync<RespuestaOpenAi>(cancellationToken);
        return LeerRespuesta(contenido);
    }

    private Uri ConstruirUri()
    {
        var baseUrl = new Uri(_proveedor.UrlBase.TrimEnd('/') + "/");
        var ruta = baseUrl.AbsolutePath == "/" ? RutaConVersion : RutaChatCompletions;
        return new Uri(baseUrl, ruta);
    }

    private JsonObject ConstruirCuerpo(SolicitudLlm solicitud)
    {
        var cuerpo = new JsonObject
        {
            ["model"] = _proveedor.Modelo,
            ["max_tokens"] = solicitud.MaximoTokensSalida,
            ["messages"] = Mensajes(solicitud)
        };
        if (solicitud.Herramientas.Count > 0)
        {
            cuerpo["tools"] = new JsonArray([.. solicitud.Herramientas.Select(Herramienta)]);
        }

        return cuerpo;
    }

    private static JsonArray Mensajes(SolicitudLlm solicitud)
    {
        var mensajes = new JsonArray { new JsonObject { ["role"] = "system", ["content"] = solicitud.PromptSistema } };
        foreach (var mensaje in solicitud.Mensajes)
        {
            mensajes.Add(Mensaje(mensaje));
        }

        return mensajes;
    }

    private static JsonObject Mensaje(MensajeLlm mensaje) => mensaje.Rol switch
    {
        RolMensaje.Herramienta => new JsonObject { ["role"] = "tool", ["tool_call_id"] = mensaje.IdLlamada, ["content"] = mensaje.Contenido },
        RolMensaje.Asistente => MensajeDeAsistente(mensaje),
        _ => new JsonObject { ["role"] = "user", ["content"] = mensaje.Contenido }
    };

    private static JsonObject MensajeDeAsistente(MensajeLlm mensaje)
    {
        var resultado = new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = string.IsNullOrEmpty(mensaje.Contenido) ? null : mensaje.Contenido
        };
        if (mensaje.Llamadas.Count > 0)
        {
            resultado["tool_calls"] = new JsonArray([.. mensaje.Llamadas.Select(LlamadaDeHerramienta)]);
        }

        return resultado;
    }

    private static JsonObject LlamadaDeHerramienta(LlamadaHerramienta llamada) => new()
    {
        ["id"] = llamada.Id,
        ["type"] = "function",
        ["function"] = new JsonObject { ["name"] = llamada.Nombre, ["arguments"] = llamada.ArgumentosJson }
    };

    private static JsonObject Herramienta(HerramientaLlm herramienta)
    {
        var propiedades = new JsonObject();
        foreach (var parametro in herramienta.Parametros)
        {
            propiedades[parametro.Nombre] = new JsonObject { ["type"] = "string", ["description"] = parametro.Descripcion };
        }

        var requeridos = herramienta.Parametros.Where(p => p.Requerido).Select(p => (JsonNode)p.Nombre).ToArray();
        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = herramienta.Nombre,
                ["description"] = herramienta.Descripcion,
                ["parameters"] = new JsonObject { ["type"] = "object", ["properties"] = propiedades, ["required"] = new JsonArray(requeridos) }
            }
        };
    }

    private static RespuestaLlm LeerRespuesta(RespuestaOpenAi? respuesta)
    {
        var contenido = respuesta ?? new RespuestaOpenAi();
        var eleccion = contenido.Choices is [var primera, ..] ? primera : new EleccionOpenAi();
        var uso = new UsoTokens(contenido.Usage.PromptTokens, contenido.Usage.CompletionTokens);
        return new RespuestaLlm(eleccion.Message.Content, LeerLlamadas(eleccion.Message), uso, EsRechazo(eleccion));
    }

    private static bool EsRechazo(EleccionOpenAi eleccion) =>
        !string.IsNullOrEmpty(eleccion.Message.Refusal) || eleccion.FinishReason == FinPorContenidoFiltrado;

    private static List<LlamadaHerramienta> LeerLlamadas(MensajeOpenAi mensaje) =>
        [.. mensaje.ToolCalls.Select(llamada => new LlamadaHerramienta(llamada.Id, llamada.Function.Name, llamada.Function.Arguments))];

    private sealed class RespuestaOpenAi
    {
        public List<EleccionOpenAi> Choices { get; init; } = [];

        public UsoOpenAi Usage { get; init; } = new();
    }

    private sealed class EleccionOpenAi
    {
        public MensajeOpenAi Message { get; init; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; init; }
    }

    private sealed class MensajeOpenAi
    {
        public string? Content { get; init; }

        public string? Refusal { get; init; }

        [JsonPropertyName("tool_calls")]
        public List<LlamadaOpenAi> ToolCalls { get; init; } = [];
    }

    private sealed class LlamadaOpenAi
    {
        public string Id { get; init; } = string.Empty;

        public FuncionOpenAi Function { get; init; } = new();
    }

    private sealed class FuncionOpenAi
    {
        public string Name { get; init; } = string.Empty;

        public string Arguments { get; init; } = "{}";
    }

    private sealed class UsoOpenAi
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; init; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; init; }
    }
}
