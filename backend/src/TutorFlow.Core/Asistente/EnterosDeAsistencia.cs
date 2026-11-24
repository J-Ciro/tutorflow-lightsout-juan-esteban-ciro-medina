using System.Globalization;
using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class EnterosDeAsistencia
{
    private const int PalabrasHastaLaPalabraClave = 3;

    private static readonly HashSet<string> PalabrasClave = new(StringComparer.Ordinal)
    {
        "falta", "faltas", "inasistencia", "inasistencias", "clase", "clases"
    };

    private static readonly HashSet<string> PalabrasQueExcluyen = new(["corte", "cortes", "semana", "semanas", "nivel", .. VocabularioDeTexto.PalabrasDeReferencia], StringComparer.Ordinal);

    private static readonly HashSet<string> Conectores = new(StringComparer.Ordinal) { "y", "e", "a" };

    public static HashSet<string> De(ResumenEstudiante resumen) =>
        resumen.Asignaturas
            .SelectMany(resultado => new int?[]
            {
                resultado.Asignatura.Asistencia.Inasistencias,
                resultado.Asignatura.Asistencia.ClasesProgramadas,
                resultado.FaltasPermitidas,
                resultado.FaltasRestantes
            })
            .OfType<int>()
            .Select(entero => entero.ToString(CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.Ordinal);

    public static IEnumerable<string> Candidatos(string texto, IReadOnlyList<TokenTexto> tokens) =>
        Enumerable.Range(0, tokens.Count)
            .Where(indice => EsEntero(tokens[indice]) && !EsPorcentaje(texto, tokens[indice]) && !EsParteDeUnNombre(texto, tokens[indice]))
            .Where(indice => !ExcluidoPorLaPalabraPrevia(tokens, indice) && TienePalabraClaveCerca(tokens, indice))
            .Select(indice => tokens[indice].Valor);

    private static bool EsEntero(TokenTexto token) => token.Valor.All(char.IsAsciiDigit);

    private static bool EsParteDeUnNombre(string texto, TokenTexto token) =>
        CaracterEn(texto, token.Inicio - 1) == '_' || CaracterEn(texto, token.Fin) == '_' || EsExtensionDeArchivo(texto, token);

    private static bool EsExtensionDeArchivo(string texto, TokenTexto token) =>
        CaracterEn(texto, token.Fin) == '.' && char.IsLetter(CaracterEn(texto, token.Fin + 1));

    private static char CaracterEn(string texto, int posicion) =>
        posicion >= 0 && posicion < texto.Length ? texto[posicion] : ' ';

    private static bool EsPorcentaje(string texto, TokenTexto token) =>
        texto[token.Fin..].TrimStart().StartsWith('%');

    private static bool ExcluidoPorLaPalabraPrevia(IReadOnlyList<TokenTexto> tokens, int indice)
    {
        var anterior = indice - 1;
        while (anterior >= 0 && (EsEntero(tokens[anterior]) || Conectores.Contains(tokens[anterior].Valor)))
        {
            anterior--;
        }

        return anterior >= 0 && PalabrasQueExcluyen.Contains(tokens[anterior].Valor);
    }

    private static bool TienePalabraClaveCerca(IReadOnlyList<TokenTexto> tokens, int indice) =>
        Enumerable.Range(Math.Max(0, indice - PalabrasHastaLaPalabraClave), (2 * PalabrasHastaLaPalabraClave) + 1)
            .Where(posicion => posicion < tokens.Count)
            .Any(posicion => PalabrasClave.Contains(tokens[posicion].Valor));
}
