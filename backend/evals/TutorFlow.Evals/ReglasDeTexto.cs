using System.Text.RegularExpressions;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Evals;

public static partial class ReglasDeTexto
{
    private const int TiempoLimiteMs = 1000;

    private static readonly HashSet<string> Preposiciones = ["a", "al", "con", "de", "del", "en", "por", "segun", "sobre"];

    private static readonly HashSet<string> ClausulasDeAtribucion =
    [
        "respaldado", "respaldada", "respaldados", "respaldadas", "se respalda", "se respaldan",
        "se basa", "se basan", "basado", "basada", "basados", "basadas",
        "se encuentra", "se encuentran", "proviene", "provienen",
        "se establece", "se establecen", "establecido", "establecida", "establecidos", "establecidas",
        "se indica", "se indican", "consulta", "ver"
    ];

    private static readonly string[] MarcasDeLatex = [@"\[", @"\(", @"\frac", @"\times", "$"];

    private static readonly Dictionary<string, string> DocumentoPorDescriptor = new()
    {
        ["reglamento academico 2025"] = Textos.DocumentoReglamentoVigente,
        ["reglamento academico"] = Textos.DocumentoReglamentoVigente,
        ["reglamento"] = Textos.DocumentoReglamentoVigente,
        ["documento de evaluacion y calificaciones"] = "evaluacion_y_calificaciones.md",
        ["documento de evaluacion"] = "evaluacion_y_calificaciones.md",
        ["evaluacion y calificaciones"] = "evaluacion_y_calificaciones.md",
        ["documento de bienestar y becas"] = "bienestar_y_becas.md",
        ["documento de bienestar"] = "bienestar_y_becas.md",
        ["bienestar y becas"] = "bienestar_y_becas.md",
        ["bienestar"] = "bienestar_y_becas.md"
    };

    public static IEnumerable<string> OracionesTruncadas(string respuesta) =>
        Oraciones(respuesta).Where(TerminaEnPreposicionOAtribucion).Select(o => $"oración truncada: '{o.Trim()}'");

    public static IEnumerable<string> Latex(string respuesta) =>
        MarcasDeLatex.Where(marca => respuesta.Contains(marca, StringComparison.Ordinal)).Select(marca => $"LaTeX: la respuesta contiene '{marca}'");

    public static IEnumerable<string> HuecosTrasUbicacion(string respuesta) =>
        HuecoTrasUbicacion().Matches(SinTildes(respuesta)).Select(m => $"hueco tras un verbo de ubicación: '{m.Value.Trim()}'");

    public static IEnumerable<string> ReferenciasSinCita(string respuesta, IReadOnlyList<CitaEval> citas)
    {
        var documentos = citas.Select(c => c.Documento).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var nombre in NombresDeDocumento().Matches(respuesta).Select(m => m.Value).Where(n => !documentos.Contains(n)))
        {
            yield return $"referencia escrita a '{nombre}' sin cita en citas";
        }

        foreach (var documento in AtribucionesGenericas(respuesta).Where(d => !documentos.Contains(d)))
        {
            yield return $"atribución genérica a '{documento}' sin cita en citas";
        }
    }

    private static IEnumerable<string> AtribucionesGenericas(string respuesta) =>
        AtribucionGenerica().Matches(NormalizadorTexto.Normalizar(respuesta))
            .Select(m => DocumentoPorDescriptor[m.Groups["descriptor"].Value]);

    private static IEnumerable<string> Oraciones(string respuesta) =>
        CorteDeOracion().Split(respuesta).Where(o => o.Trim().Length > 0);

    private static bool TerminaEnPreposicionOAtribucion(string oracion)
    {
        var palabras = NormalizadorTexto.Normalizar(oracion).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 0)
        {
            return false;
        }

        var ultima = palabras[^1];
        var ultimasDos = palabras.Length > 1 ? $"{palabras[^2]} {ultima}" : ultima;
        return Preposiciones.Contains(ultima) || ClausulasDeAtribucion.Contains(ultima) || ClausulasDeAtribucion.Contains(ultimasDos);
    }

    private static string SinTildes(string texto) =>
        texto.ToLowerInvariant().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u');

    [GeneratedRegex(@"\b(?:esta|estan|figura|figuran|aparece|aparecen)\s*(?:[,;:.!?)]|$)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, matchTimeoutMilliseconds: TiempoLimiteMs)]
    private static partial Regex HuecoTrasUbicacion();

    [GeneratedRegex(@"(?<=[.!?;])\s+|\r?\n", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteMs)]
    private static partial Regex CorteDeOracion();

    [GeneratedRegex(@"[\p{L}\p{N}_-]+\.md\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteMs)]
    private static partial Regex NombresDeDocumento();

    [GeneratedRegex(
        @"\b(?:segun|de acuerdo con|conforme a|conforme al|proviene de|proviene del|provienen de|provienen del|se basa en|se basan en|basado en|basada en|basados en|basadas en|se encuentra en|se encuentran en|respaldado por|respaldada por|respaldados por|respaldadas por|se respalda en|se respaldan en|establecido en|establecida en|establecidos en|establecidas en) (?:el |la |los |las )?(?:(?:seccion|secciones|articulo|articulos) \d+(?: \d+)* (?:de|del|de la) )?(?<descriptor>reglamento academico 2025|reglamento academico|reglamento|documento de evaluacion y calificaciones|documento de evaluacion|evaluacion y calificaciones|documento de bienestar y becas|documento de bienestar|bienestar y becas|bienestar)\b",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: TiempoLimiteMs)]
    private static partial Regex AtribucionGenerica();
}
