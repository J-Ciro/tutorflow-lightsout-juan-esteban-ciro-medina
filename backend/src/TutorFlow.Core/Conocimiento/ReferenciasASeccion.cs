using System.Text.RegularExpressions;

namespace TutorFlow.Core.Conocimiento;

public static partial class ReferenciasASeccion
{
    private const string Reemplazo = " ";

    public static string Quitar(string textoPlegado) => Referencia().Replace(textoPlegado, Reemplazo);

    [GeneratedRegex(@"(?<![a-z0-9])(?:secciones|seccion|articulos|articulo|art\.?|§)\s*\d+(?:\.\d+)*(?:\s*(?:,|y|e)\s*\d+(?:\.\d+)*)*", RegexOptions.None, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Referencia();
}
