namespace TutorFlow.Core.Autorizacion;

internal static class ConteoDeExistencia
{
    private const int MaximoDeRellenoDelConteo = 2;
    private const int MaximoEntreVerboYEstado = 2;
    private const int MaximoDeRellenoTrasElGrupo = 2;
    private const int SinPalabrasEntre = 0;

    public static bool Hay(FrasesDelTexto frases) =>
        frases.Ocurrencias(VocabularioAgregados.Contadores)
            .SelectMany(contador => frases.FinesTrasRelleno(
                contador.Fin, VocabularioAgregados.RellenosDelConteo, MaximoDeRellenoDelConteo, VocabularioAgregados.Grupo))
            .SelectMany(finDelGrupo => frases.FinesTrasRelleno(
                finDelGrupo, VocabularioAgregados.RellenosTrasElGrupo, MaximoDeRellenoTrasElGrupo, VocabularioAgregados.VerbosDeExistencia))
            .Any(finDelVerbo => EsExistenciaDeEstado(frases, finDelVerbo));

    private static bool EsExistenciaDeEstado(FrasesDelTexto frases, int finDelVerbo) =>
        !frases.HayDestinoEnLosSiguientes(finDelVerbo, SinPalabrasEntre, VocabularioAgregados.Derecho)
        && frases.HayDestinoEnLosSiguientes(finDelVerbo, MaximoEntreVerboYEstado, VocabularioAgregados.Estados);
}
