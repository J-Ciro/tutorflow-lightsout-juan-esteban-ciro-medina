namespace TutorFlow.Core.Autorizacion;

public static class DetectorAgregados
{
    private const int MaximoEntreSuperlativoYNota = 2;
    private const int MaximoEntrePosicionYVerbo = 3;
    private const int MaximoDeRellenoDelPromedio = 2;
    private const int MaximoDeRellenoDelListado = 3;
    private const int MaximoDeRellenoDelPuesto = 2;
    private const int MaximoDeRellenoDelComparativo = 2;

    public static bool PideAgregados(string textoNormalizado)
    {
        var frases = new FrasesDelTexto(textoNormalizado);
        return NivelUno(frases) || NivelDos(frases);
    }

    private static bool NivelUno(FrasesDelTexto frases) =>
        frases.Contiene(VocabularioAgregados.FuertesSueltas)
        || PosicionPropia(frases)
        || PromedioDeColectivo(frases)
        || PluralSinPosesivo(frases)
        || PuestoDeColectivo(frases);

    private static bool PuestoDeColectivo(FrasesDelTexto frases) =>
        frases.SeguidaDeRellenoY(
            VocabularioAgregados.PrimerosYUltimos,
            VocabularioAgregados.RellenosDelPuesto,
            MaximoDeRellenoDelPuesto,
            VocabularioAgregados.Colectivos);

    private static bool NivelDos(FrasesDelTexto frases) =>
        (frases.Contiene(VocabularioAgregados.Grupo) && NivelDosConGrupo(frases))
        || ConteoDeExistencia.Hay(frases);

    private static bool NivelDosConGrupo(FrasesDelTexto frases) =>
        SuperlativoSobreNota(frases)
        || Listado(frases)
        || frases.Contiene(VocabularioAgregados.VerbosDeListadoYResultado)
        || frases.ContieneRaiz(VocabularioAgregados.RaizDeComparar)
        || ComparativoSeguidoDeGrupo(frases);

    private static bool ComparativoSeguidoDeGrupo(FrasesDelTexto frases) =>
        frases.SeguidaDeRellenoY(
            VocabularioAgregados.Comparativos,
            VocabularioAgregados.RellenosDelComparativo,
            MaximoDeRellenoDelComparativo,
            VocabularioAgregados.Grupo);

    private static bool PosicionPropia(FrasesDelTexto frases) =>
        ProximidadDeFrases.Hay(
            frases.Ocurrencias(VocabularioAgregados.Posiciones),
            frases.Ocurrencias(VocabularioAgregados.VerbosDePosicion),
            MaximoEntrePosicionYVerbo);

    private static bool PromedioDeColectivo(FrasesDelTexto frases) =>
        frases.SeguidaDeRellenoY(
            VocabularioAgregados.PromediosDeGrupo,
            VocabularioAgregados.RellenosDelPromedio,
            MaximoDeRellenoDelPromedio,
            VocabularioAgregados.Colectivos);

    private static bool PluralSinPosesivo(FrasesDelTexto frases) =>
        !PreguntaPropia(frases)
        && ProximidadDeFrases.Hay(
            SinPosesivoAntes(frases, VocabularioAgregados.PluralesSuperlativos, VocabularioAgregados.PosesivoPlural),
            frases.Ocurrencias(VocabularioAgregados.NotasPlurales),
            MaximoEntreSuperlativoYNota);

    private static bool PreguntaPropia(FrasesDelTexto frases) =>
        frases.Contiene(VocabularioAgregados.Posesivos)
        && !frases.ContieneSeguidaDe(["mi"], VocabularioAgregados.Colectivos);

    private static bool SuperlativoSobreNota(FrasesDelTexto frases) =>
        ProximidadDeFrases.Hay(
            SinPosesivoAntes(frases, VocabularioAgregados.Superlativos, VocabularioAgregados.Posesivos),
            SinPosesivoAntes(frases, VocabularioAgregados.Notas, VocabularioAgregados.Posesivos),
            MaximoEntreSuperlativoYNota);

    private static bool Listado(FrasesDelTexto frases) =>
        frases.SeguidaDeRellenoY(
            VocabularioAgregados.Listadores,
            VocabularioAgregados.RellenosDelListado,
            MaximoDeRellenoDelListado,
            VocabularioAgregados.Grupo);

    private static IReadOnlyList<OcurrenciaDeFrase> SinPosesivoAntes(
        FrasesDelTexto frases,
        IEnumerable<string> vocabulario,
        IReadOnlyCollection<string> posesivos) =>
        [.. frases.Ocurrencias(vocabulario).Where(ocurrencia => !posesivos.Contains(frases.PalabraAntesDe(ocurrencia)))];
}
