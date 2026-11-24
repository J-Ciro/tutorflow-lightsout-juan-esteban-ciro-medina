using System.Text.RegularExpressions;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal static partial class AtribucionGenerica
{
    private const string GrupoReglamento = "reglamento";
    private const string GrupoEvaluacion = "evaluacion";
    private const string GrupoBienestar = "bienestar";
    private const string FinDePalabra = @"(?![\p{L}\p{N}_-])";
    private const string Numero = @"\d+(?:\.\d+)*";
    private const string Conector =
        @"(?<![\p{L}\p{N}_-])(?:seg[uú]n|de\s+acuerdo\s+con|conforme\s+al?|proviene(?:n)?\s+del?|se\s+basan?\s+en|basad[oa]s?\s+en"
        + @"|se\s+encuentran?\s+en|respaldad[oa]s?\s+por|se\s+respaldan?\s+en|establecid[oa]s?\s+en)(?:\s+(?:el|la|los|las))?\s+";
    private const string SeccionDelDocumento =
        @"(?:(?:secci[oó]n(?:es)?|art[ií]culos?|art\.?|§)\s*" + Numero + @"(?:\s*(?:,|y|e)\s*" + Numero + @")*\s+(?:de\s+la|del|de)\s+)?";
    private const string Descriptor =
        @"(?:(?<" + GrupoReglamento + @">reglamento\s+acad[eé]mico(?:\s+2025)?|reglamento)"
        + @"|(?<" + GrupoEvaluacion + @">documento\s+de\s+evaluaci[oó]n(?:\s+y\s+calificaciones)?|evaluaci[oó]n\s+y\s+calificaciones)"
        + @"|(?<" + GrupoBienestar + @">documento\s+de\s+bienestar(?:\s+y\s+becas)?|bienestar\s+y\s+becas|bienestar))";

    private static readonly Dictionary<string, string> DocumentoPorGrupo = new()
    {
        [GrupoReglamento] = ReglasAplicadas.DocumentoReglamento,
        [GrupoEvaluacion] = ReglasAplicadas.DocumentoEvaluacion,
        [GrupoBienestar] = ReglasAplicadas.DocumentoBienestar
    };

    public static string Marcar(string texto, IReadOnlyList<Cita> citas) =>
        Atribucion().Replace(texto, coincidencia => TieneCita(coincidencia, citas) ? coincidencia.Value : ReferenciasEscritas.Marca.ToString());

    private static bool TieneCita(Match coincidencia, IReadOnlyList<Cita> citas) =>
        DocumentoPorGrupo.Any(grupo => coincidencia.Groups[grupo.Key].Success && citas.Any(cita => cita.Documento == grupo.Value));

    [GeneratedRegex(Conector + SeccionDelDocumento + Descriptor + FinDePalabra, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TiempoLimiteRegex.Milisegundos)]
    private static partial Regex Atribucion();
}
