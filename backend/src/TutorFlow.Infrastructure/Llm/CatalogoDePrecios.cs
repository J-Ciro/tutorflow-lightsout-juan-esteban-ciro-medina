using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TutorFlow.Core;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed partial class CatalogoDePrecios
{
    public const int BytesMaximos = 10_485_760;
    private const decimal PrecioMaximoPorToken = 0.001m;

    private static readonly Dictionary<ProveedorLlm, string> PrefijosPorProveedor = new()
    {
        [ProveedorLlm.Gemini] = "google",
        [ProveedorLlm.Anthropic] = "anthropic",
        [ProveedorLlm.OpenAi] = "openai"
    };

    private readonly IReadOnlyDictionary<string, PrecioModelo> _porId;

    private CatalogoDePrecios(IReadOnlyDictionary<string, PrecioModelo> porId) => _porId = porId;

    public static CatalogoDePrecios Leer(byte[] cuerpo)
    {
        using var documento = JsonDocument.Parse(cuerpo);
        if (!documento.RootElement.TryGetProperty("data", out var datos) || datos.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("El catálogo no trae data.");
        }

        return new CatalogoDePrecios(Indexar(datos.EnumerateArray()));
    }

    public PrecioModelo? Buscar(ProveedorLlm proveedor, string modelo)
    {
        if (proveedor == ProveedorLlm.OpenRouter)
        {
            return _porId.GetValueOrDefault(modelo);
        }

        var conPrefijo = PrefijosPorProveedor.TryGetValue(proveedor, out var prefijo)
            ? _porId.GetValueOrDefault($"{prefijo}/{modelo}")
            : null;
        return conPrefijo ?? UnicoConSufijo(modelo);
    }

    private PrecioModelo? UnicoConSufijo(string modelo)
    {
        var sufijo = $"/{modelo}";
        var candidatos = _porId.Where(par => par.Key.EndsWith(sufijo, StringComparison.Ordinal)).Take(2).ToList();
        return candidatos.Count == 1 ? candidatos[0].Value : null;
    }

    private static Dictionary<string, PrecioModelo> Indexar(IEnumerable<JsonElement> entradas)
    {
        var conId = entradas.Select(entrada => (Id: IdDe(entrada), Entrada: entrada)).Where(par => par.Id is not null).ToList();
        var repetidos = conId.GroupBy(par => par.Id).Where(grupo => grupo.Count() > 1).Select(grupo => grupo.Key).ToHashSet();
        return conId
            .Where(par => !repetidos.Contains(par.Id))
            .Select(par => (Id: par.Id!, Precio: PrecioDe(par.Entrada)))
            .Where(par => par.Precio is not null)
            .ToDictionary(par => par.Id, par => par.Precio!, StringComparer.Ordinal);
    }

    private static string? IdDe(JsonElement entrada) =>
        entrada.ValueKind == JsonValueKind.Object && entrada.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String
            ? id.GetString()
            : null;

    private static PrecioModelo? PrecioDe(JsonElement entrada)
    {
        if (!entrada.TryGetProperty("pricing", out var precios) || precios.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var entradaPorToken = PrecioPorToken(precios, "prompt");
        var salidaPorToken = PrecioPorToken(precios, "completion");
        return entradaPorToken is { } e && salidaPorToken is { } s
            ? new PrecioModelo(e * CalculadoraCosto.TokensPorMillon, s * CalculadoraCosto.TokensPorMillon)
            : null;
    }

    private static decimal? PrecioPorToken(JsonElement precios, string campo) =>
        precios.TryGetProperty(campo, out var valor) ? Convertir(TextoDe(valor)) : null;

    private static string? TextoDe(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.String => valor.GetString(),
        JsonValueKind.Number => valor.GetRawText(),
        _ => null
    };

    private static decimal? Convertir(string? texto)
    {
        if (texto is null || !FormatoDecimal().IsMatch(texto))
        {
            return null;
        }

        var valido = decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero);
        return valido && numero <= PrecioMaximoPorToken ? numero : null;
    }

    [GeneratedRegex(@"^[0-9]+(\.[0-9]+)?\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex FormatoDecimal();
}
