namespace TutorFlow.Core.Autorizacion;

internal static class ProximidadDeFrases
{
    public static bool Hay(IEnumerable<OcurrenciaDeFrase> primeras, IReadOnlyCollection<OcurrenciaDeFrase> segundas, int maximoEntre) =>
        primeras.Any(primera => Cercanas(primera, segundas, maximoEntre));

    private static bool Cercanas(OcurrenciaDeFrase primera, IEnumerable<OcurrenciaDeFrase> segundas, int maximoEntre) =>
        segundas.Any(segunda => PalabrasEntre(primera, segunda) is >= 0 and var entre && entre <= maximoEntre);

    private static int PalabrasEntre(OcurrenciaDeFrase primera, OcurrenciaDeFrase segunda)
    {
        if (segunda.Inicio >= primera.Fin)
        {
            return segunda.Inicio - primera.Fin;
        }

        return primera.Inicio >= segunda.Fin ? primera.Inicio - segunda.Fin : -1;
    }
}
