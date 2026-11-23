using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Conocimiento;

public static class RaicesDeContenido
{
    private const int LongitudDeRaiz = 4;

    private static readonly HashSet<string> PalabrasVacias = new(StringComparer.Ordinal)
    {
        "cual", "cuales", "como", "cuando", "donde", "quien", "cuanto", "cuantos", "cuanta", "cuantas",
        "puedo", "tengo", "tiene", "tienen", "sobre", "para", "unos", "unas", "pero", "esta", "este", "esto",
        "dice", "dicen", "hacer", "hace", "debe", "puede", "pueden", "estoy", "estan", "dime", "dame", "muestra"
    };

    private static readonly HashSet<string> PalabrasGenericasDelCorpus = new(StringComparer.Ordinal)
    {
        "reglamento", "reglamentos", "documento", "documentos", "articulo", "articulos", "seccion", "secciones",
        "capitulo", "corporacion", "horizonte"
    };

    public static IEnumerable<string> De(string texto) =>
        NormalizadorTexto.Normalizar(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(EsPalabraDeContenido)
            .Select(palabra => palabra[..LongitudDeRaiz])
            .Distinct(StringComparer.Ordinal);

    private static bool EsPalabraDeContenido(string palabra) =>
        palabra.Length >= LongitudDeRaiz
        && !PalabrasVacias.Contains(palabra)
        && !PalabrasGenericasDelCorpus.Contains(palabra);
}
