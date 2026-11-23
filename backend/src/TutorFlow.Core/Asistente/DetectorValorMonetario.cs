using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public static partial class DetectorValorMonetario
{
    private const string SeparadoresFinales = ".,";

    public static bool HayValorNoRespaldado(string respuesta, IReadOnlyCollection<string> textosFuente)
    {
        var respaldados = textosFuente
            .SelectMany(texto => Numero().Matches(texto).Select(coincidencia => coincidencia.Value))
            .ToHashSet(StringComparer.Ordinal);
        return ValoresMonetarios(respuesta).Any(valor => !respaldados.Contains(valor));
    }

    private static IEnumerable<string> ValoresMonetarios(string respuesta)
    {
        var plegada = TokenizadorTexto.Plegado(respuesta);
        return new[] { ConSimbolo(), ConMoneda(), ConMonedaDespues(), ConSeparadorDeMiles() }
            .SelectMany(patron => patron.Matches(plegada))
            .Select(coincidencia => coincidencia.Groups["n"].Value.TrimEnd(SeparadoresFinales.ToCharArray()))
            .Distinct(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\$\s*(?<n>\d[\d.,]*)", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConSimbolo();

    [GeneratedRegex(@"\b(?:cop|usd)\s*(?<n>\d[\d.,]*)", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConMoneda();

    [GeneratedRegex(@"(?<![\d.,])(?<n>\d[\d.,]*)\s*(?:pesos|dolares|cop|usd)\b", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConMonedaDespues();

    [GeneratedRegex(@"(?<![\d.,])(?<n>\d{1,3}(?:[.,]\d{3})+)(?!\d)", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex ConSeparadorDeMiles();

    [GeneratedRegex(@"\d[\d.,]*\d|\d", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Numero();
}
