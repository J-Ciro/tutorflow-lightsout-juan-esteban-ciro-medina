using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Conocimiento;

public static class RaicesDeContenido
{
    private const int LongitudDeRaiz = ConsultaDeRecuperacion.LongitudDeRaiz;

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

    public static IReadOnlyCollection<string> DeApoyo(string texto) =>
        [.. De(texto).Concat(RaicesNumericas.De(texto))];

    private static bool EsPalabraDeContenido(string palabra) =>
        palabra.Length >= LongitudDeRaiz
        && !PalabrasVacias.Contiene(palabra)
        && !PalabrasGenericasDelCorpus.Contains(palabra);
}
