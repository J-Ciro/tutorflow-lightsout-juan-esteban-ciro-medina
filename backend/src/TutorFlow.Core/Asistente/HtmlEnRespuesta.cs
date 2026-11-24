using System.Globalization;
using System.Text.RegularExpressions;

namespace TutorFlow.Core.Asistente;

internal static partial class HtmlEnRespuesta
{
    private const string Atributos = @"(?:\s+[\w:-]+(?:=(?:""[^""]*""|'[^']*'|[^\s>]+))?)*";

    public static string AMarkdown(string texto) =>
        texto.Contains('<', StringComparison.Ordinal)
            ? QuitarEtiquetas(EncabezadosDeMarkdown(NegritasDeLinea(texto)))
            : texto;

    private static string NegritasDeLinea(string texto) =>
        NegritaDeLinea().Replace(texto, "**${texto}**");

    private static string EncabezadosDeMarkdown(string texto) =>
        Encabezado().Replace(texto, ConAlmohadillas);

    private static string QuitarEtiquetas(string texto) =>
        Etiqueta().Replace(texto, string.Empty);

    private static string ConAlmohadillas(Match encabezado)
    {
        var nivel = int.Parse(encabezado.Groups["nivel"].Value, CultureInfo.InvariantCulture);
        return $"{new string('#', nivel)} {encabezado.Groups["texto"].Value}";
    }

    [GeneratedRegex(@"^[ \t]*<(b|strong)>(?<texto>[^<\r\n]+)</\1>[ \t]*$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex NegritaDeLinea();

    [GeneratedRegex(@"<h(?<nivel>[1-6])>(?<texto>[^<\r\n]+)</h\k<nivel>>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Encabezado();

    [GeneratedRegex(@"</?[a-z][a-z0-9]*" + Atributos + @"\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Etiqueta();
}
