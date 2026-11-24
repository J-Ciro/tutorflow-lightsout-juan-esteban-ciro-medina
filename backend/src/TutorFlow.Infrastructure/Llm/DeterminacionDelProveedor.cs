namespace TutorFlow.Infrastructure.Llm;

public sealed record ConflictoDeProveedor(ProveedorLlm PorModelo, ProveedorLlm PorLlave);

internal sealed record Determinacion(ProveedorLlm Proveedor, ConflictoDeProveedor? Conflicto = null);

internal static class DeterminacionDelProveedor
{
    private static readonly Dictionary<string, ProveedorLlm> ProveedoresExplicitos =
        new Dictionary<string, ProveedorLlm>(StringComparer.OrdinalIgnoreCase)
        {
            ["anthropic"] = ProveedorLlm.Anthropic,
            ["openai"] = ProveedorLlm.OpenAi,
            ["groq"] = ProveedorLlm.Groq,
            ["openrouter"] = ProveedorLlm.OpenRouter,
            ["gemini"] = ProveedorLlm.Gemini,
            ["ollama"] = ProveedorLlm.Ollama,
            ["openai-compatible"] = ProveedorLlm.OpenAiCompatible
        };

    private static readonly (string Prefijo, ProveedorLlm Proveedor)[] PrefijosDeModelo =
    [
        ("claude-", ProveedorLlm.Anthropic),
        ("gemini-", ProveedorLlm.Gemini),
        ("gpt-", ProveedorLlm.OpenAi),
        ("o1", ProveedorLlm.OpenAi),
        ("o3", ProveedorLlm.OpenAi),
        ("o4", ProveedorLlm.OpenAi)
    ];

    private static readonly (string Prefijo, ProveedorLlm Proveedor)[] PistasEspecificasDeLlave =
    [
        ("sk-ant-", ProveedorLlm.Anthropic),
        ("sk-or-", ProveedorLlm.OpenRouter),
        ("gsk_", ProveedorLlm.Groq),
        ("AIza", ProveedorLlm.Gemini)
    ];

    private const string PrefijoGenericoDeLlave = "sk-";
    private const char SeparadorDeOpenRouter = '/';

    public static Determinacion Determinar(string? proveedorExplicito, string? llave, string? modelo, string? urlBase)
    {
        if (proveedorExplicito is not null && ProveedoresExplicitos.TryGetValue(proveedorExplicito, out var elegido))
        {
            return new Determinacion(Validar(elegido, urlBase));
        }

        return llave is null ? new Determinacion(ProveedorLlm.Ollama) : PorModeloYLlave(llave, modelo, urlBase);
    }

    private static Determinacion PorModeloYLlave(string llave, string? modelo, string? urlBase)
    {
        var pista = PistaEspecifica(llave);
        return PorModelo(modelo) is { } deModelo
            ? ConfrontarConLaPista(deModelo, pista)
            : new Determinacion(pista ?? Generica(llave) ?? PorUrl(urlBase));
    }

    private static Determinacion ConfrontarConLaPista(ProveedorLlm deModelo, ProveedorLlm? pista) =>
        pista is { } deLlave && deLlave != deModelo
            ? new Determinacion(ProveedorLlm.Desconocido, new ConflictoDeProveedor(deModelo, deLlave))
            : new Determinacion(deModelo);

    private static ProveedorLlm PorUrl(string? urlBase) =>
        urlBase is null ? ProveedorLlm.Desconocido : ProveedorLlm.OpenAiCompatible;

    private static ProveedorLlm? PorModelo(string? modelo)
    {
        if (modelo is null)
        {
            return null;
        }

        var coincidencia = PrefijosDeModelo.FirstOrDefault(p => modelo.StartsWith(p.Prefijo, StringComparison.OrdinalIgnoreCase));
        return coincidencia.Prefijo is not null ? coincidencia.Proveedor : ConBarra(modelo);
    }

    private static ProveedorLlm? ConBarra(string modelo) =>
        modelo.Contains(SeparadorDeOpenRouter, StringComparison.Ordinal) ? ProveedorLlm.OpenRouter : null;

    private static ProveedorLlm? PistaEspecifica(string llave)
    {
        var coincidencia = PistasEspecificasDeLlave.FirstOrDefault(p => llave.StartsWith(p.Prefijo, StringComparison.Ordinal));
        return coincidencia.Prefijo is not null ? coincidencia.Proveedor : null;
    }

    private static ProveedorLlm? Generica(string llave) =>
        llave.StartsWith(PrefijoGenericoDeLlave, StringComparison.Ordinal) ? ProveedorLlm.OpenAi : null;

    private static ProveedorLlm Validar(ProveedorLlm proveedor, string? urlBase) =>
        proveedor == ProveedorLlm.OpenAiCompatible && urlBase is null ? ProveedorLlm.Desconocido : proveedor;
}
