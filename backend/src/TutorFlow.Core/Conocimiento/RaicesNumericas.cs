using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Conocimiento;

public static partial class RaicesNumericas
{
    public static IEnumerable<string> De(string texto)
    {
        var plegado = ReferenciaASeccion().Replace(TokenizadorTexto.Plegado(texto), " ");
        var porcentajes = Porcentaje().Matches(plegado).Select(coincidencia => SinEspacios(coincidencia.Value)).ToList();
        var sinPorcentajes = Porcentaje().Replace(plegado, " ");
        var notas = FormaDeNota().Matches(sinPorcentajes).Select(coincidencia => coincidencia.Value);
        return porcentajes.Concat(notas).Distinct(StringComparer.Ordinal);
    }

    private static string SinEspacios(string valor) => valor.Replace(" ", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"(?<!\d)(?<!\d\.)\d+(?:\.\d{1,2})?\s*%")]
    private static partial Regex Porcentaje();

    [GeneratedRegex(@"(?<!\d)(?<!\d\.)\d\.\d{1,2}(?!\d)(?!\.\d)")]
    private static partial Regex FormaDeNota();

    [GeneratedRegex(@"(?:articulo|seccion)\s+\d+(?:\.\d+)?")]
    private static partial Regex ReferenciaASeccion();
}
