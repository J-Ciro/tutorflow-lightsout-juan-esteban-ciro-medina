using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static partial class IntroduccionDelDesglose
{
    public static string SinCifras(string texto) =>
        string.Join("\n", SaltoDeLinea().Split(texto).Select(SinOracionesConCifras).OfType<string>()).Trim();

    private static string? SinOracionesConCifras(string linea)
    {
        var conservada = string.Concat(CorteDeOracion().Split(linea).Where(oracion => !TieneCifrasDeFaltas(oracion)));
        if (conservada == linea)
        {
            return linea;
        }

        return string.IsNullOrWhiteSpace(conservada) ? null : conservada.Trim();
    }

    private static bool TieneCifrasDeFaltas(string oracion)
    {
        var normalizada = ComaDecimal.APunto(oracion);
        return EnterosDeAsistencia.Candidatos(normalizada, TokenizadorTexto.TokenizarConCifras(normalizada)).Any();
    }

    [GeneratedRegex(@"\r?\n", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex SaltoDeLinea();

    [GeneratedRegex(@"(?<=[.!?])(?=\s|$)", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex CorteDeOracion();
}
