using System.Net;
using System.Text.Json;
using Anthropic;
using Anthropic.Core;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed class ClienteLlmAnthropic : IClienteLlm, IDisposable
{
    private const string MotivoRechazo = "refusal";
    private const int ReintentosMaximos = 0;
    private const string CampoTemperatura = "temperature";
    private const string EncabezadoRetryAfter = "Retry-After";
    private const string MensajeDelProveedor = "El proveedor del LLM respondió con un error.";

    private readonly AnthropicClient _cliente;
    private readonly string _modelo;
    private string? _retryAfterDelProveedor;

    public ClienteLlmAnthropic(HttpClient httpClient, ProveedorResuelto proveedor, string llave)
    {
        _modelo = proveedor.Modelo;
        _cliente = new AnthropicClient
        {
            HttpClient = httpClient,
            ApiKey = llave,
            BaseUrl = proveedor.UrlBase,
            MaxRetries = ReintentosMaximos,
            Handlers = [Handler.Create(RegistrarRetryAfterAsync)],
            Timeout = httpClient.Timeout
        };
    }

    public void Dispose() => _cliente.Dispose();

    public async Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken)
    {
        var parametros = new MessageCreateParams
        {
            Model = _modelo,
            MaxTokens = solicitud.MaximoTokensSalida,
            System = solicitud.PromptSistema,
            Messages = Mensajes(solicitud.Mensajes),
            Tools = solicitud.Herramientas.Select(Herramienta).ToList()
        };
        try
        {
            var mensaje = await _cliente.Messages.Create(ConTemperatura(parametros, solicitud.Temperatura), cancellationToken);
            return LeerRespuesta(mensaje);
        }
        catch (AnthropicException)
        {
            throw new DependenciaNoDisponibleException(MensajeDelProveedor, _retryAfterDelProveedor);
        }
    }

    private async Task<HttpResponseMessage> RegistrarRetryAfterAsync(HttpRequestMessage peticion, NextHandler siguiente, CancellationToken cancellationToken)
    {
        var respuesta = await siguiente(peticion, cancellationToken);
        _retryAfterDelProveedor = RetryAfterDe(respuesta);
        return respuesta;
    }

    private static string? RetryAfterDe(HttpResponseMessage respuesta) =>
        respuesta.StatusCode == HttpStatusCode.TooManyRequests && respuesta.Headers.TryGetValues(EncabezadoRetryAfter, out var valores)
            ? valores.FirstOrDefault()
            : null;

    private static MessageCreateParams ConTemperatura(MessageCreateParams parametros, decimal temperatura)
    {
        var cuerpo = parametros.RawBodyData.ToDictionary(campo => campo.Key, campo => campo.Value);
        cuerpo[CampoTemperatura] = JsonSerializer.SerializeToElement(temperatura);
        return MessageCreateParams.FromRawUnchecked(parametros.RawHeaderData, parametros.RawQueryData, cuerpo);
    }

    private static List<MessageParam> Mensajes(IReadOnlyList<MensajeLlm> mensajes)
    {
        var resultado = new List<MessageParam>();
        var resultadosPendientes = new List<ContentBlockParam>();
        foreach (var mensaje in mensajes)
        {
            if (mensaje.Rol == RolMensaje.Herramienta)
            {
                resultadosPendientes.Add(new ToolResultBlockParam { ToolUseID = mensaje.IdLlamada ?? string.Empty, Content = mensaje.Contenido });
                continue;
            }

            VaciarResultados(resultado, resultadosPendientes);
            resultado.Add(mensaje.Rol == RolMensaje.Asistente ? MensajeDeAsistente(mensaje) : new UserMessageParam { Content = mensaje.Contenido });
        }

        VaciarResultados(resultado, resultadosPendientes);
        return resultado;
    }

    private static void VaciarResultados(List<MessageParam> resultado, List<ContentBlockParam> resultadosPendientes)
    {
        if (resultadosPendientes.Count == 0)
        {
            return;
        }

        resultado.Add(new UserMessageParam { Content = new List<ContentBlockParam>(resultadosPendientes) });
        resultadosPendientes.Clear();
    }

    private static AssistantMessageParam MensajeDeAsistente(MensajeLlm mensaje)
    {
        var bloques = new List<ContentBlockParam>();
        if (!string.IsNullOrEmpty(mensaje.Contenido))
        {
            bloques.Add(new TextBlockParam { Text = mensaje.Contenido });
        }

        bloques.AddRange(mensaje.Llamadas.Select(llamada => (ContentBlockParam)new ToolUseBlockParam
        {
            ID = llamada.Id,
            Name = llamada.Nombre,
            Input = EntradaDe(llamada.ArgumentosJson)
        }));
        return new AssistantMessageParam { Content = bloques };
    }

    private static Dictionary<string, JsonElement> EntradaDe(string argumentosJson)
    {
        using var documento = JsonDocument.Parse(argumentosJson);
        return documento.RootElement.ValueKind == JsonValueKind.Object
            ? documento.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
            : [];
    }

    private static ToolUnion Herramienta(HerramientaLlm herramienta) =>
        new Tool
        {
            Name = herramienta.Nombre,
            Description = herramienta.Descripcion,
            InputSchema = new()
            {
                Properties = herramienta.Parametros.ToDictionary(
                    p => p.Nombre,
                    p => JsonSerializer.SerializeToElement(new { type = "string", description = p.Descripcion })),
                Required = [.. herramienta.Parametros.Where(p => p.Requerido).Select(p => p.Nombre)]
            }
        };

    private static RespuestaLlm LeerRespuesta(Message mensaje)
    {
        var uso = new UsoTokens((int)mensaje.Usage.InputTokens, (int)mensaje.Usage.OutputTokens);
        if (mensaje.StopReason == MotivoRechazo)
        {
            return new RespuestaLlm(null, [], uso, true);
        }

        var texto = string.Concat(mensaje.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        var llamadas = mensaje.Content
            .Select(b => b.Value)
            .OfType<ToolUseBlock>()
            .Select(b => new LlamadaHerramienta(b.ID, b.Name, JsonSerializer.Serialize(b.Input)))
            .ToList();
        return new RespuestaLlm(texto.Length == 0 ? null : texto, llamadas, uso, false);
    }
}
