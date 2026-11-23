using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Pruebas.Soporte;

public static partial class PartesDelPrompt
{
    public static string Ejemplos(string prompt)
    {
        var bloque = BloqueDeEjemplos().Match(prompt);
        return bloque.Success
            ? bloque.Groups["cuerpo"].Value
            : throw new InvalidOperationException("El prompt de sistema no tiene el bloque <ejemplos>...</ejemplos>.");
    }

    public static string Instrucciones(string prompt) => BloqueDeEjemplos().Replace(prompt, string.Empty);

    public static IReadOnlyList<string> CasosDeEjemplo(string prompt) =>
        [.. CasoDeEjemplo().Matches(Ejemplos(prompt)).Select(caso => caso.Groups["cuerpo"].Value)];

    public static string[] Palabras(string texto) =>
        NormalizadorTexto.Normalizar(texto).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static string PrimerasPalabras(string texto, int cantidad) =>
        string.Join(' ', Palabras(texto).Take(cantidad));

    public static HashSet<string> Fragmentos(string texto, int palabrasPorFragmento)
    {
        var palabras = Palabras(texto);
        return Enumerable.Range(0, Math.Max(0, palabras.Length - palabrasPorFragmento + 1))
            .Select(inicio => string.Join(' ', palabras[inicio..(inicio + palabrasPorFragmento)]))
            .ToHashSet(StringComparer.Ordinal);
    }

    [GeneratedRegex("<ejemplos>(?<cuerpo>.*?)</ejemplos>", RegexOptions.Singleline | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BloqueDeEjemplos();

    [GeneratedRegex("<ejemplo>(?<cuerpo>.*?)</ejemplo>", RegexOptions.Singleline | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CasoDeEjemplo();
}
