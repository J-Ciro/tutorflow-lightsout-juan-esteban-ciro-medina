using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

internal static class LlamadasConDatoOpaco
{
    private static readonly HashSet<string> PropiedadesConocidas = ["id", "type", "function"];

    public static LlamadaHerramienta Traducir(FunctionCallContent llamada) =>
        new(
            llamada.CallId,
            llamada.Name,
            JsonSerializer.Serialize(llamada.Arguments ?? new Dictionary<string, object?>()),
            llamada.RawRepresentation is ChatToolCall cruda && TraeDatoAdicional(cruda) ? new DatoOpacoDelProveedor(cruda) : null);

    private static bool TraeDatoAdicional(ChatToolCall cruda)
    {
        using var documento = JsonDocument.Parse(ModelReaderWriter.Write(cruda));
        return documento.RootElement.EnumerateObject().Any(propiedad => !PropiedadesConocidas.Contains(propiedad.Name));
    }

    public static ChatMessage ConDatoOpaco(ChatMessage reconstruido, MensajeLlm mensaje)
    {
        if (mensaje.Llamadas.Count > 0 && mensaje.Llamadas.All(Coincide))
        {
            reconstruido.RawRepresentation = MensajeCrudo(mensaje);
        }

        return reconstruido;
    }

    private static AssistantChatMessage MensajeCrudo(MensajeLlm mensaje)
    {
        var crudo = new AssistantChatMessage(mensaje.Llamadas.Select(llamada => (ChatToolCall)llamada.DatoOpaco!.Valor));
        if (!string.IsNullOrEmpty(mensaje.Contenido))
        {
            crudo.Content.Add(ChatMessageContentPart.CreateTextPart(mensaje.Contenido));
        }

        return crudo;
    }

    private static bool Coincide(LlamadaHerramienta llamada) =>
        llamada.DatoOpaco?.Valor is ChatToolCall cruda
        && cruda.Id == llamada.Id
        && cruda.FunctionName == llamada.Nombre
        && MismosArgumentos(cruda.FunctionArguments, llamada.ArgumentosJson);

    private static bool MismosArgumentos(BinaryData? crudos, string ejecutados)
    {
        try
        {
            return JsonNode.DeepEquals(JsonNode.Parse(crudos?.ToString() ?? "{}"), JsonNode.Parse(ejecutados));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
