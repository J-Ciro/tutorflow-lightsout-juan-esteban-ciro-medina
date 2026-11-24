using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed class ClienteLlmCompatibleOpenAi : IClienteLlm
{
    private const string LlaveParaServidoresSinAutenticacion = "sin-llave";
    private const string SegmentoDeVersion = "v1/";
    private const int ReintentosMaximos = 0;
    private const string EncabezadoRetryAfter = "Retry-After";
    private const string MensajeDelProveedor = "El proveedor del LLM respondió con un error.";

    private readonly IChatClient _cliente;
    private readonly string _modelo;
    private readonly bool _controlaContexto;
    private readonly ILogger? _logger;

    public ClienteLlmCompatibleOpenAi(HttpClient httpClient, ProveedorResuelto proveedor, string? llave, ILogger? logger = null)
    {
        _modelo = proveedor.Modelo;
        _logger = logger;
        _controlaContexto = proveedor.Proveedor == ProveedorLlm.Ollama;
        var opciones = new OpenAIClientOptions
        {
            Endpoint = ConstruirEndpoint(proveedor.UrlBase),
            Transport = new HttpClientPipelineTransport(httpClient),
            RetryPolicy = new ClientRetryPolicy(ReintentosMaximos)
        };
        var credencial = new ApiKeyCredential(string.IsNullOrWhiteSpace(llave) ? LlaveParaServidoresSinAutenticacion : llave);
        _cliente = new OpenAIClient(credencial, opciones).GetChatClient(proveedor.Modelo).AsIChatClient();
    }

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        var opciones = new ChatOptions
        {
            ModelId = _modelo,
            MaxOutputTokens = solicitud.MaximoTokensSalida,
            Temperature = (float)solicitud.Temperatura,
            Tools = [.. solicitud.Herramientas.Select(Declarar)]
        };
        try
        {
            var respuesta = await _cliente.GetResponseAsync(Mensajes(solicitud), opciones, cancellationToken);
            return ConControlDeContexto(solicitud, Traducir(respuesta), respuesta.Usage?.InputTokenCount is not null);
        }
        catch (ClientResultException excepcion)
        {
            DiagnosticoDelProveedor.Registrar(_logger, excepcion.Status, CuerpoDe(excepcion));
            throw new DependenciaNoDisponibleException(MensajeDelProveedor, RetryAfterDe(excepcion));
        }
    }

    private static string? CuerpoDe(ClientResultException excepcion) =>
        excepcion.GetRawResponse()?.Content?.ToString();

    private static string? RetryAfterDe(ClientResultException excepcion) =>
        excepcion.Status == (int)HttpStatusCode.TooManyRequests && excepcion.GetRawResponse() is { } respuesta && respuesta.Headers.TryGetValue(EncabezadoRetryAfter, out var valor)
            ? valor
            : null;

    private RespuestaLlm ConControlDeContexto(SolicitudLlm solicitud, RespuestaLlm respuesta, bool usoReportado) =>
        _controlaContexto && usoReportado ? respuesta with { ContextoTruncado = EstimadorDeContexto.Evaluar(solicitud, respuesta.Uso) } : respuesta;

    private static Uri ConstruirEndpoint(string urlBase)
    {
        var raiz = new Uri(urlBase.TrimEnd('/') + "/");
        return raiz.AbsolutePath == "/" ? new Uri(raiz, SegmentoDeVersion) : raiz;
    }

    private static List<ChatMessage> Mensajes(SolicitudLlm solicitud)
    {
        var mensajes = new List<ChatMessage> { new(ChatRole.System, solicitud.PromptSistema) };
        mensajes.AddRange(solicitud.Mensajes.Select(Mensaje));
        return mensajes;
    }

    private static ChatMessage Mensaje(MensajeLlm mensaje) => mensaje.Rol switch
    {
        RolMensaje.Herramienta => new ChatMessage(ChatRole.Tool, [new FunctionResultContent(mensaje.IdLlamada ?? string.Empty, mensaje.Contenido)]),
        RolMensaje.Asistente => MensajeDeAsistente(mensaje),
        _ => new ChatMessage(ChatRole.User, mensaje.Contenido)
    };

    private static ChatMessage MensajeDeAsistente(MensajeLlm mensaje)
    {
        var contenidos = new List<AIContent>();
        if (!string.IsNullOrEmpty(mensaje.Contenido))
        {
            contenidos.Add(new TextContent(mensaje.Contenido));
        }

        contenidos.AddRange(mensaje.Llamadas.Select(llamada =>
            new FunctionCallContent(llamada.Id, llamada.Nombre, ArgumentosDe(llamada.ArgumentosJson))));
        return new ChatMessage(ChatRole.Assistant, contenidos);
    }

    private static Dictionary<string, object?> ArgumentosDe(string argumentosJson)
    {
        using var documento = JsonDocument.Parse(argumentosJson);
        return documento.RootElement.ValueKind == JsonValueKind.Object
            ? documento.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.Clone())
            : [];
    }

    private static AIFunctionDeclaration Declarar(HerramientaLlm herramienta)
    {
        var propiedades = herramienta.Parametros.ToDictionary(
            parametro => parametro.Nombre,
            parametro => new { type = "string", description = parametro.Descripcion });
        var esquema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = propiedades,
            required = herramienta.Parametros.Where(p => p.Requerido).Select(p => p.Nombre).ToArray()
        });
        return AIFunctionFactory.CreateDeclaration(herramienta.Nombre, herramienta.Descripcion, esquema);
    }

    private static RespuestaLlm Traducir(ChatResponse respuesta)
    {
        var rechazada = EsRechazo(respuesta);
        var texto = rechazada ? null : respuesta.Text;
        return new RespuestaLlm(
            string.IsNullOrEmpty(texto) ? null : texto,
            LlamadasDe(respuesta),
            UsoDe(respuesta.Usage),
            rechazada);
    }

    private static bool EsRechazo(ChatResponse respuesta) =>
        respuesta.FinishReason == ChatFinishReason.ContentFilter
        || (respuesta.RawRepresentation is OpenAI.Chat.ChatCompletion completado && !string.IsNullOrEmpty(completado.Refusal));

    private static List<LlamadaHerramienta> LlamadasDe(ChatResponse respuesta) =>
        [.. respuesta.Messages
            .SelectMany(mensaje => mensaje.Contents)
            .OfType<FunctionCallContent>()
            .Select(llamada => new LlamadaHerramienta(llamada.CallId, llamada.Name, JsonSerializer.Serialize(llamada.Arguments ?? new Dictionary<string, object?>())))];

    private static UsoTokens UsoDe(UsageDetails? uso) =>
        new((int)(uso?.InputTokenCount ?? 0), (int)(uso?.OutputTokenCount ?? 0));
}
