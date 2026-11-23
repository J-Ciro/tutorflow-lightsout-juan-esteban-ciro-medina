using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record MaterialConfidencial(
    string Marcador,
    IReadOnlyCollection<string> TextosConfidenciales,
    IReadOnlyCollection<string> NombresDeHerramientas);

public static class DetectorFugaConfiguracion
{
    public const int PalabrasMinimasDeFragmento = 8;

    public static bool HayFuga(string respuesta, MaterialConfidencial material, IReadOnlyCollection<string> textosEntregados) =>
        Contiene(respuesta, material.Marcador)
        || material.NombresDeHerramientas.Any(nombre => Contiene(respuesta, nombre))
        || ComparteFragmentoConfidencial(respuesta, material.TextosConfidenciales, textosEntregados);

    private static bool Contiene(string texto, string valor) =>
        texto.Contains(valor, StringComparison.OrdinalIgnoreCase);

    private static bool ComparteFragmentoConfidencial(
        string respuesta,
        IReadOnlyCollection<string> textosConfidenciales,
        IReadOnlyCollection<string> textosEntregados)
    {
        var permitidos = TextosFijos().Concat(textosEntregados).SelectMany(FragmentosDe).ToHashSet(StringComparer.Ordinal);
        var confidenciales = textosConfidenciales.SelectMany(FragmentosDe).ToHashSet(StringComparer.Ordinal);
        return FragmentosDe(respuesta).Any(fragmento => confidenciales.Contains(fragmento) && !permitidos.Contains(fragmento));
    }

    private static string[] TextosFijos() =>
        [Textos.SinInformacion, Textos.NegativaEstudiante, Textos.NegativaCoordinador, Textos.NoVerificable];

    private static HashSet<string> FragmentosDe(string texto)
    {
        string[] palabras = [.. TokenizadorTexto.Valores(texto)];
        return Enumerable.Range(0, Math.Max(0, palabras.Length - PalabrasMinimasDeFragmento + 1))
            .Select(inicio => string.Join(' ', palabras[inicio..(inicio + PalabrasMinimasDeFragmento)]))
            .ToHashSet(StringComparer.Ordinal);
    }
}
