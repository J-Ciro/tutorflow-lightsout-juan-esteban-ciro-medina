using System.Text.Json;

namespace TutorFlow.Core.Asistente;

public static class ArgumentosHerramienta
{
    public static string? LeerTexto(string argumentosJson, string nombre)
    {
        try
        {
            using var documento = JsonDocument.Parse(argumentosJson);
            return Extraer(documento.RootElement, nombre);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Extraer(JsonElement raiz, string nombre)
    {
        if (raiz.ValueKind != JsonValueKind.Object || !raiz.TryGetProperty(nombre, out var valor))
        {
            return null;
        }

        return valor.ValueKind == JsonValueKind.String ? valor.GetString() ?? string.Empty : valor.ToString();
    }
}
