using System.Text.RegularExpressions;

namespace TutorFlow.Core.Asistente;

internal static partial class ComaDecimal
{
    public static string APunto(string texto) => Cifra().Replace(texto, "$1.$2");

    [GeneratedRegex(@"(?<![\d.,])(\d+),(\d{1,2})(?![\d,])", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Cifra();
}
