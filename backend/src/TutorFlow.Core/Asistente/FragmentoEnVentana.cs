namespace TutorFlow.Core.Asistente;

internal static class FragmentoEnVentana
{
    public const int PalabrasDelFragmento = DetectorFugaConfiguracion.PalabrasMinimasDeFragmento;
    public const int PalabrasDeLaVentana = 16;

    public static IEnumerable<string[]> Fragmentos(string[] palabras) =>
        Enumerable.Range(0, Math.Max(0, palabras.Length - PalabrasDelFragmento + 1))
            .Select(inicio => palabras[inicio..(inicio + PalabrasDelFragmento)]);

    public static bool Aparece(string[] fragmento, string[] respuesta) =>
        Enumerable.Range(0, respuesta.Length).Any(inicio => respuesta[inicio] == fragmento[0] && EnOrdenDesde(fragmento, respuesta, inicio));

    private static bool EnOrdenDesde(string[] fragmento, string[] respuesta, int inicio)
    {
        var siguiente = 1;
        var limite = Math.Min(respuesta.Length, inicio + PalabrasDeLaVentana);
        for (var posicion = inicio + 1; posicion < limite && siguiente < fragmento.Length; posicion++)
        {
            siguiente += respuesta[posicion] == fragmento[siguiente] ? 1 : 0;
        }

        return siguiente == fragmento.Length;
    }
}
