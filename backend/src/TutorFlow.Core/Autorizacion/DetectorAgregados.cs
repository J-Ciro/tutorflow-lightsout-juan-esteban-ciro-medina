using System.Text.RegularExpressions;

namespace TutorFlow.Core.Autorizacion;

public static partial class DetectorAgregados
{
    public static bool PideAgregados(string textoNormalizado) =>
        PatronesAgregados().IsMatch(textoNormalizado);

    [GeneratedRegex(
        @"\b(mejor promedio|peor promedio|ranking|que puesto|en que lugar quede|por encima de la media"
        + @"|promedio (del curso|del programa|de la carrera)|que estudiantes|todos los estudiantes"
        + @"|otros estudiantes|cuantos estudiantes)\b"
        + @"|\bcompar\w*\b.*\bcompaner\w*\b|\blist\w*\b.*\bestudiantes\b",
        RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex PatronesAgregados();
}
