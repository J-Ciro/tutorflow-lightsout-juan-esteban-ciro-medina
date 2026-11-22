namespace TutorFlow.Infrastructure.Llm;

public enum ProveedorLlm
{
    Anthropic,
    OpenAi,
    Groq,
    OpenRouter,
    Gemini,
    Ollama,
    OpenAiCompatible,
    Desconocido
}

public enum AdaptadorLlm
{
    Anthropic,
    CompatibleOpenAi,
    NoConfigurado
}

public sealed class ModeloConfigurado
{
    public string Nombre { get; init; } = string.Empty;
    public decimal PrecioEntradaPorMillon { get; init; }
    public decimal PrecioSalidaPorMillon { get; init; }
}

public sealed class ConfiguracionProveedor
{
    public string UrlBase { get; init; } = string.Empty;
    public string ModeloPorDefecto { get; init; } = string.Empty;
    public IReadOnlyList<ModeloConfigurado> Modelos { get; init; } = [];
}

public sealed class OpcionesLlm
{
    public const string Seccion = "Llm";

    public IReadOnlyDictionary<string, ConfiguracionProveedor> Proveedores { get; init; } = new Dictionary<string, ConfiguracionProveedor>();
}

public sealed record EntornoLlm(
    string? Llave,
    string? Proveedor,
    string? UrlBase,
    string? Modelo,
    decimal? PrecioEntrada,
    decimal? PrecioSalida);

public sealed record ProveedorResuelto(
    ProveedorLlm Proveedor,
    AdaptadorLlm Adaptador,
    string UrlBase,
    string Modelo,
    decimal PrecioEntradaPorMillon,
    decimal PrecioSalidaPorMillon,
    bool PrecioDesconocido);
