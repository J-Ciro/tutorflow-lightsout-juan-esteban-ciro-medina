using System.Text.RegularExpressions;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static partial class UmbralDeCifra
{
    private const int PalabrasDelRequisito = 4;

    private static readonly HashSet<string> Comparadores = new(StringComparer.Ordinal)
    {
        "menor", "mayor", "inferior", "superior", "minimo", "minima", "maximo", "maxima", "supera", "superar", "entre",
        "<", ">", "<=", ">="
    };

    private static readonly HashSet<(string, string)> ComparadoresDeDosPalabras = new()
    {
        ("o", "igual"), ("al", "menos"), ("menos", "de"), ("mas", "de")
    };

    private static readonly HashSet<string> Enlaces = new(StringComparer.Ordinal) { "a", "de", "que" };

    public static bool Es(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        PrecedidaDeComparador(tokens, cifra) || SigueARequisito(tokens, cifra) || UmbralNormativo.Es(tokens, cifra);

    private static bool PrecedidaDeComparador(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        TerminaComparador(tokens, cifra - 1) || (cifra > 0 && Enlaces.Contains(tokens[cifra - 1].Valor) && TerminaComparador(tokens, cifra - 2));

    private static bool TerminaComparador(IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice >= 0
        && (Comparadores.Contains(tokens[indice].Valor)
            || (indice > 0 && ComparadoresDeDosPalabras.Contains((tokens[indice - 1].Valor, tokens[indice].Valor))));

    private static bool SigueARequisito(IReadOnlyList<TokenTexto> tokens, int cifra) =>
        RequisitoPrevio().IsMatch(string.Join(' ', tokens.Take(cifra).TakeLast(PalabrasDelRequisito).Select(token => token.Valor)));

    [GeneratedRegex(@"(?:^| )necesitas(?: (?:una|la))? definitiva de\z", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: TiempoLimiteRegex.Milisegundos)]
    private static partial Regex RequisitoPrevio();
}
