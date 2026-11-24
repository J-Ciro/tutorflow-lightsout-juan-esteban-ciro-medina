using System.Globalization;

namespace TutorFlow.Api;

internal static class ValorDeMensaje
{
    private const int LongitudMaxima = 64;
    private const string PuntosSuspensivos = "…";

    private static readonly Dictionary<char, string> SecuenciasDeEscape = new()
    {
        ['"'] = "\\\"",
        ['\\'] = "\\\\",
        ['\n'] = "\\n",
        ['\r'] = "\\r",
        ['\t'] = "\\t"
    };

    public static string De(string? valor)
    {
        var escapado = string.Concat((valor ?? string.Empty).Select(Escapar));
        return escapado.Length <= LongitudMaxima ? escapado : escapado[..Corte(escapado)] + PuntosSuspensivos;
    }

    private static int Corte(string escapado) =>
        char.IsHighSurrogate(escapado[LongitudMaxima - 1]) ? LongitudMaxima - 1 : LongitudMaxima;

    private static bool EsInvisible(char caracter) =>
        char.IsControl(caracter) || char.GetUnicodeCategory(caracter) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator or UnicodeCategory.Format;

    private static string Escapar(char caracter) =>
        SecuenciasDeEscape.TryGetValue(caracter, out var secuencia) ? secuencia
        : EsInvisible(caracter) ? $"\\u{(int)caracter:X4}"
        : caracter.ToString();
}
