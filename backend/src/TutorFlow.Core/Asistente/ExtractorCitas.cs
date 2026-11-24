using System.Text.RegularExpressions;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static partial class ExtractorCitas
{
    private const string PrefijoArticulo = "Artículo";
    private const string PrefijoSeccion = "Sección";
    private const string SeccionAnexo = "Anexo";

    public static IReadOnlyList<Cita> De(string texto) =>
        CitaEnTexto().Matches(texto).SelectMany(CitasDe).Distinct().ToList();

    public static string SinCitas(string texto) =>
        CitaEnTexto().Replace(texto, " ");

    private static IEnumerable<Cita> CitasDe(Match coincidencia)
    {
        var documento = coincidencia.Groups["doc"].Value;
        return coincidencia.Groups["anexo"].Success
            ? [new Cita(documento, SeccionAnexo)]
            : Numero().Matches(coincidencia.Groups["nums"].Value).Select(numero => new Cita(documento, $"{Prefijo(coincidencia.Groups["tipo"].Value)} {numero.Value}"));
    }

    private static string Prefijo(string palabra) =>
        palabra.StartsWith("Art", StringComparison.OrdinalIgnoreCase) ? PrefijoArticulo : PrefijoSeccion;

    [GeneratedRegex(
        @"(?<doc>[A-Za-z0-9_]+\.md)\s*·\s*(?:(?<tipo>Art[ií]culos?|Secci[oó]n(?:es)?)\s+(?<nums>\d+(?:\.\d+)?(?:\s*(?:,|y|e)\s*\d+(?:\.\d+)?)*)|(?<anexo>Anexo))",
        RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CitaEnTexto();

    [GeneratedRegex(@"\d+(?:\.\d+)?", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Numero();
}
