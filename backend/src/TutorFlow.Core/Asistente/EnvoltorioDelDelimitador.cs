using System.Text.RegularExpressions;

namespace TutorFlow.Core.Asistente;

internal static partial class EnvoltorioDelDelimitador
{
    public static string Quitar(string texto) =>
        texto.Contains(FormateadorDatos.MarcaDelimitadorOriginal, StringComparison.Ordinal) ? Marcas().Replace(texto, string.Empty).Trim() : texto;

    [GeneratedRegex(@"<<<DATOS_NO_CONFIABLES(?: fuente=""[^""\r\n]*"")?>>>|FIN_DATOS_NO_CONFIABLES>>>", RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Marcas();
}
