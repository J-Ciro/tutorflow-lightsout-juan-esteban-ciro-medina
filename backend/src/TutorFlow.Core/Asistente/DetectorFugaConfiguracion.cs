using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public sealed record MaterialConfidencial(
    string Marcador,
    IReadOnlyCollection<string> TextosConfidenciales,
    IReadOnlyCollection<string> NombresDeHerramientas);

public static class DetectorFugaConfiguracion
{
    public const int PalabrasMinimasDeFragmento = 8;
    public const int CaracteresMinimosDelMarcador = 8;

    public static bool HayFuga(string respuesta, MaterialConfidencial material, IReadOnlyCollection<string> textosEntregados) =>
        ContieneAlgunSecreto(respuesta, Secretos(material))
        || ComparteFragmentoConfidencial(respuesta, material.TextosConfidenciales, textosEntregados);

    private static IEnumerable<string> Secretos(MaterialConfidencial material) =>
        [PrefijoDelMarcador(material.Marcador), FormateadorDatos.MarcaDelimitadorOriginal, .. material.NombresDeHerramientas];

    private static string PrefijoDelMarcador(string marcador)
    {
        var compacto = Compactar(marcador);
        return compacto[..Math.Min(compacto.Length, CaracteresMinimosDelMarcador)];
    }

    private static bool ContieneAlgunSecreto(string respuesta, IEnumerable<string> secretos)
    {
        var compacta = Compactar(respuesta);
        return secretos.Select(Compactar).Where(secreto => secreto.Length > 0).Any(secreto => compacta.Contains(secreto, StringComparison.Ordinal));
    }

    private static string Compactar(string texto) =>
        string.Concat(texto.Where(caracter => !EsSeparadorOInvisible(caracter))).ToLowerInvariant();

    private static bool EsSeparadorOInvisible(char caracter) =>
        caracter is '_' or '-' or '.' || char.GetUnicodeCategory(caracter) == System.Globalization.UnicodeCategory.Format;

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
