using System.Globalization;
using System.Text;

namespace TutorFlow.Api.Tests.Dobles;

public static class RaicesDeContenido
{
    private const int LongitudMinima = 4;

    private static readonly HashSet<string> Excluidas = new(StringComparer.Ordinal)
    {
        "cual", "cuales", "como", "cuando", "donde", "quien", "cuanto", "cuantos", "cuanta", "cuantas", "puedo", "tengo", "tiene", "tienen",
        "sobre", "para", "unos", "unas", "pero", "esta", "este", "esto", "dice", "dicen", "hacer", "hace", "debe", "puede", "pueden",
        "estoy", "estan", "dime", "dame", "muestra",
        "reglamento", "reglamentos", "documento", "documentos", "articulo", "articulos", "seccion", "secciones", "capitulo", "corporacion", "horizonte"
    };

    public static IReadOnlySet<string> De(string texto) =>
        Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length >= LongitudMinima && !Excluidas.Contains(p))
            .Select(p => p[..LongitudMinima])
            .ToHashSet(StringComparer.Ordinal);

    public static int Compartidas(string a, string b) => De(a).Intersect(De(b)).Count();

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormKD).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinMarcas = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(sinMarcas.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray());
    }
}
