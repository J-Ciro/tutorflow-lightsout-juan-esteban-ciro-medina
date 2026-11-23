using System.Text.RegularExpressions;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static partial class ExtractorCitas
{
    private const string PrefijoArticulo = "Artículo";
    private const string PrefijoSeccion = "Sección";

    public static IReadOnlyList<Cita> De(string texto) =>
        CitaEnTexto().Matches(texto)
            .Select(coincidencia => new Cita(coincidencia.Groups["doc"].Value, Normalizar(coincidencia.Groups["sec"].Value)))
            .Distinct()
            .ToList();

    public static string SinCitas(string texto) =>
        CitaEnTexto().Replace(texto, " ");

    private static string Normalizar(string seccion)
    {
        var partes = seccion.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length < 2 ? seccion : $"{Prefijo(partes[0])} {partes[1].Trim()}";
    }

    private static string Prefijo(string palabra) =>
        palabra.StartsWith("Art", StringComparison.OrdinalIgnoreCase) ? PrefijoArticulo : PrefijoSeccion;

    [GeneratedRegex(@"(?<doc>[A-Za-z0-9_]+\.md)\s*·\s*(?<sec>Art[ií]culo\s+\d+|Secci[oó]n\s+\d+(?:\.\d+)?|Anexo)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CitaEnTexto();
}
