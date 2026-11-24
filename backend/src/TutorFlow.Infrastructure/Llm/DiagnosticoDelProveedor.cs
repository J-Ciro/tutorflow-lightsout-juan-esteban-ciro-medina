using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TutorFlow.Infrastructure.Llm;

public static partial class DiagnosticoDelProveedor
{
    public const int LongitudMaximaDelCodigo = 64;
    private static readonly string[] CamposDeCodigo = ["status", "code", "type"];

    public static void Registrar(ILogger? logger, int estado, string? cuerpo)
    {
        if (logger is null)
        {
            return;
        }

        var codigo = CodigoDe(cuerpo);
        if (codigo.Length == 0)
        {
            RegistrarSoloEstado(logger, estado);
            return;
        }

        RegistrarConCodigo(logger, estado, codigo);
    }

    public static string CodigoDe(string? cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return string.Empty;
        }

        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            return PrimerCodigo(documento.RootElement);
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string PrimerCodigo(JsonElement raiz) =>
        ObjetoError(raiz) is { } error
            ? CamposDeCodigo.Select(campo => TextoDe(error, campo)).FirstOrDefault(texto => texto.Length > 0) ?? string.Empty
            : string.Empty;

    private static JsonElement? ObjetoError(JsonElement raiz) =>
        raiz.ValueKind == JsonValueKind.Object && raiz.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
            ? error
            : null;

    private static string TextoDe(JsonElement error, string campo) =>
        error.TryGetProperty(campo, out var valor) ? Sanear(Texto(valor) ?? string.Empty) : string.Empty;

    private static string? Texto(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.String => valor.GetString(),
        JsonValueKind.Number => valor.GetRawText(),
        _ => null
    };

    private static string Sanear(string texto)
    {
        var permitido = new StringBuilder();
        foreach (var caracter in texto.Where(EsPermitido).Take(LongitudMaximaDelCodigo))
        {
            permitido.Append(caracter);
        }

        return permitido.ToString();
    }

    private static bool EsPermitido(char caracter) =>
        char.IsAsciiLetterOrDigit(caracter) || caracter is '_' or '.' or '-';

    [LoggerMessage(Level = LogLevel.Warning, Message = "El proveedor del LLM respondió con error {Estado}.")]
    private static partial void RegistrarSoloEstado(ILogger logger, int estado);

    [LoggerMessage(Level = LogLevel.Warning, Message = "El proveedor del LLM respondió con error {Estado} {Codigo}.")]
    private static partial void RegistrarConCodigo(ILogger logger, int estado, string codigo);
}
