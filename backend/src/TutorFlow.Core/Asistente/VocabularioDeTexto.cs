namespace TutorFlow.Core.Asistente;

internal static class VocabularioDeTexto
{
    public static readonly HashSet<string> Negaciones = new(StringComparer.Ordinal) { "no", "ni", "sin" };

    public static readonly HashSet<string> PalabrasDeReferencia = new(StringComparer.Ordinal)
    {
        "seccion", "secciones", "articulo", "articulos", "art"
    };
}
