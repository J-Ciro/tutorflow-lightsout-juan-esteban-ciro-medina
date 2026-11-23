using System.Diagnostics;
using System.Globalization;

namespace TutorFlow.Evals;

public sealed record EntornoFaseV(string FechaUtc, string Commit, string Proveedor, string Modelo, string Temperatura, string Semilla, string ContextoDeOllama)
{
    private const string NoDisponible = "n/a";
    private const string ProveedorOffline = "doble (offline)";
    private const string ProveedorLocal = "ollama";
    private const string ModeloOffline = "doble";
    private const string ModeloLocal = "qwen2.5:7b";
    private const string ModeloAnthropic = "claude-haiku-5-5";
    private const string ModeloDelProveedor = "(por defecto del proveedor)";
    private const string TemperaturaPorDefecto = "0";
    private const string SemillaPorDefecto = "1";
    private const int TiempoDeGitMs = 5000;
    private const int LongitudMinimaDelHash = 7;
    private const int LongitudMaximaDelHash = 40;

    private static readonly (string Prefijo, string Proveedor)[] PrefijosDeLlave =
    [
        ("sk-ant-", "anthropic"),
        ("sk-or-", "openrouter"),
        ("gsk_", "groq"),
        ("AIza", "gemini"),
        ("sk-", "openai")
    ];

    public static EntornoFaseV Leer(bool offline, string raizDelRepositorio)
    {
        var proveedor = offline ? ProveedorOffline : ProveedorElegido();
        return new EntornoFaseV(
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            CommitActual(raizDelRepositorio),
            proveedor,
            ModeloDe(proveedor),
            Variable("Llm__Temperatura", TemperaturaPorDefecto),
            Variable("Llm__Semilla", SemillaPorDefecto),
            ContextoDe(proveedor));
    }

    private static string ContextoDe(string proveedor) =>
        proveedor == ProveedorLocal ? Variable("OLLAMA_CONTEXT_LENGTH", NoDisponible) : NoDisponible;

    private static string Variable(string nombre, string porDefecto) => Variable(nombre) ?? porDefecto;

    private static string? Variable(string nombre)
    {
        var valor = Environment.GetEnvironmentVariable(nombre);
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private static string ProveedorElegido()
    {
        var forzado = Variable("LLM_PROVEEDOR");
        return forzado ?? ProveedorDeLaLlave(Variable("LLM_API_KEY"));
    }

    private static string ProveedorDeLaLlave(string? llave) =>
        llave is null
            ? ProveedorLocal
            : PrefijosDeLlave.FirstOrDefault(p => llave.StartsWith(p.Prefijo, StringComparison.Ordinal)).Proveedor ?? ProveedorLocal;

    private static string ModeloDe(string proveedor) =>
        Variable("LLM_MODELO") ?? proveedor switch
        {
            ProveedorOffline => ModeloOffline,
            ProveedorLocal => ModeloLocal,
            "anthropic" => ModeloAnthropic,
            _ => ModeloDelProveedor
        };

    private static string CommitActual(string raiz)
    {
        try
        {
            return HashDe(raiz);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return NoDisponible;
        }
    }

    private static string HashDe(string raiz)
    {
        using var proceso = Process.Start(new ProcessStartInfo("git", "rev-parse --short HEAD")
        {
            WorkingDirectory = raiz,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new System.ComponentModel.Win32Exception();
        var salida = proceso.StandardOutput.ReadToEnd().Trim();
        proceso.WaitForExit(TiempoDeGitMs);
        return EsHash(salida) ? salida : NoDisponible;
    }

    private static bool EsHash(string texto) =>
        texto.Length is >= LongitudMinimaDelHash and <= LongitudMaximaDelHash && texto.All(Uri.IsHexDigit);
}
