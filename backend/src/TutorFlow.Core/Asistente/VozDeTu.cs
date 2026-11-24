using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class VozDeTu
{
    private const int PalabrasHastaElCampo = 2;

    private static readonly HashSet<string> Segunda = new(StringComparer.Ordinal) { "tu", "tus", "tienes", "te" };

    private static readonly HashSet<string> Campos = new(StringComparer.Ordinal)
    {
        "definitiva", "definitivas", "promedio", "falta", "faltas", "inasistencia", "inasistencias", "clase", "clases",
        "perdida", "perdidas", "aprobada", "aprobadas", "reprobada", "reprobadas", "prueba"
    };

    private static readonly HashSet<string> Referencias = new(StringComparer.Ordinal)
    {
        "programa", "programas", "estudiante", "estudiantes", "coordinador", "coordinadora"
    };

    public static bool Hay(IReadOnlyList<TokenTexto> tokens) =>
        Enumerable.Range(0, tokens.Count).Any(indice => Segunda.Contains(tokens[indice].Valor) && !EsReferencia(tokens, indice) && TieneCampoCerca(tokens, indice));

    private static bool EsReferencia(IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice + 1 < tokens.Count && Referencias.Contains(tokens[indice + 1].Valor);

    private static bool TieneCampoCerca(IReadOnlyList<TokenTexto> tokens, int indice) =>
        VentanaDePalabras.Posiciones(tokens.Count, indice, PalabrasHastaElCampo)
            .Where(posicion => posicion != indice)
            .Any(posicion => Campos.Contains(tokens[posicion].Valor));
}
