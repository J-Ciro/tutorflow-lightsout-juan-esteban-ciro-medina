using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class UmbralNormativo
{
    private const int PalabrasHastaLaCifra = 3;

    private static readonly HashSet<string> Campos = new(StringComparer.Ordinal) { "promedio", "nota", "definitiva", "calificacion" };
    private static readonly HashSet<string> Extremos = new(StringComparer.Ordinal) { "minimo", "minima", "maximo", "maxima" };
    private static readonly HashSet<string> Copulas = new(StringComparer.Ordinal) { "es", "son" };
    private static readonly HashSet<string> Requisitos = new(StringComparer.Ordinal) { "requiere", "requieren" };
    private static readonly HashSet<string> MarcadoresPersonales = new(StringComparer.Ordinal) { "tu", "tus", "te", "tienes", "mi", "mis", "tengo" };

    public static bool Es(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        DespuesDelExtremo(tokens, cifra) || DespuesDeSeRequiere(tokens, cifra);

    private static bool DespuesDelExtremo(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        cifra >= PalabrasHastaLaCifra
        && Copulas.Contains(tokens[cifra - 1].Valor)
        && Extremos.Contains(tokens[cifra - 2].Valor)
        && Campos.Contains(tokens[cifra - PalabrasHastaLaCifra].Valor);

    private static bool DespuesDeSeRequiere(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        Enumerable.Range(1, PalabrasHastaLaCifra + 1)
            .Select(distancia => cifra - distancia)
            .Where(posicion => posicion >= 1 && Requisitos.Contains(tokens[posicion].Valor) && tokens[posicion - 1].Valor == "se")
            .Any(posicion => LimpioEntre(tokens, posicion, cifra));

    private static bool LimpioEntre(IReadOnlyList<TokenTexto> tokens, int requisito, int cifra) =>
        Enumerable.Range(requisito + 1, cifra - requisito - 1)
            .All(posicion => !MarcadoresPersonales.Contains(tokens[posicion].Valor) && !char.IsAsciiDigit(tokens[posicion].Valor[0]));
}
