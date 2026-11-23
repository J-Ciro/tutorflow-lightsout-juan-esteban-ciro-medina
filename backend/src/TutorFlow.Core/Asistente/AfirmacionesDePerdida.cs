using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class AfirmacionesDePerdida
{
    private static readonly HashSet<string> Inasistencias = new(StringComparer.Ordinal)
    {
        "inasistencia", "inasistencias"
    };

    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public AfirmacionesDePerdida(IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _catalogo = catalogo;
    }

    public bool SonVerificables(
        string respuesta,
        Rol rol,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos) =>
        TramosDePersonas.DeRespuesta(respuesta, rol, idsAutorizados, directorio)
            .SelectMany(oracion => oracion.Tramos)
            .All(tramo => TramoEsVerificable(tramo, CatalogoAsignaturas.Vigente(_catalogo, directorio), resumenesLeidos));

    private static bool TramoEsVerificable(TramoDePersona tramo, IReadOnlyList<AsignaturaCatalogo> catalogo, IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        var exigencias = Exigencias(tramo.Tokens, catalogo);
        return exigencias.Count == 0
            || (resumenesLeidos.TryGetValue(tramo.PersonaId, out var resumen) && exigencias.All(exigencia => exigencia.Cumple(resumen)));
    }

    private static List<ExigenciaDePerdida> Exigencias(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        var formas = FormasDePerdida.En([.. tokens.Select(token => token.Valor)]);
        if (formas.Count == 0)
        {
            return [];
        }

        var nombradas = PrimerasApariciones(tokens, catalogo);
        var asociadas = nombradas.Select(nombrada => (Nombrada: nombrada, Forma: FormaMasCercana(formas, nombrada))).ToList();
        var porAsignatura = asociadas
            .Where(asociada => !asociada.Forma.Negada)
            .Select(asociada => new ExigenciaDePerdida(asociada.Nombrada.Clave, PorInasistencia(tokens, nombradas, asociada.Nombrada)));
        var sinAsignatura = formas
            .Where(forma => forma.Nominal && !forma.Negada && asociadas.All(asociada => asociada.Forma != forma))
            .Select(forma => new ExigenciaDePerdida(null, forma.PorInasistencia));
        return [.. porAsignatura.Concat(sinAsignatura)];
    }

    private static List<AparicionDeAsignatura> PrimerasApariciones(IReadOnlyList<TokenTexto> tokens, IReadOnlyList<AsignaturaCatalogo> catalogo) =>
        [.. CatalogoAsignaturas.Apariciones(tokens, catalogo)
            .GroupBy(aparicion => aparicion.Clave)
            .Select(grupo => grupo.MinBy(aparicion => aparicion.Inicio)!)
            .OrderBy(aparicion => aparicion.Inicio)];

    private static FormaDePerdida FormaMasCercana(IReadOnlyList<FormaDePerdida> formas, AparicionDeAsignatura asignatura) =>
        formas.MinBy(forma => (Distancia(forma, asignatura), forma.Fin >= asignatura.Inicio))!;

    private static int Distancia(FormaDePerdida forma, AparicionDeAsignatura asignatura) =>
        forma.Fin < asignatura.Inicio ? asignatura.Inicio - forma.Fin : Math.Max(0, forma.Inicio - asignatura.Fin);

    private static bool PorInasistencia(
        IReadOnlyList<TokenTexto> tokens,
        List<AparicionDeAsignatura> nombradas,
        AparicionDeAsignatura asignatura)
    {
        var inasistencias = Enumerable.Range(0, tokens.Count).Where(indice => Inasistencias.Contains(tokens[indice].Valor)).ToList();
        var siguiente = nombradas.Where(nombrada => nombrada.Inicio > asignatura.Inicio).Select(nombrada => nombrada.Inicio).DefaultIfEmpty(int.MaxValue).Min();
        return inasistencias.Any(indice => indice < nombradas[0].Inicio || (indice >= asignatura.Inicio && indice < siguiente));
    }

    private sealed record ExigenciaDePerdida(string? Clave, bool PorInasistencia)
    {
        public bool Cumple(ResumenEstudiante resumen) =>
            resumen.Asignaturas.Any(resultado => (Clave is null || AmbitoPersonal.Clave(resultado.Asignatura.Codigo) == Clave) && Satisface(resultado));

        private bool Satisface(ResultadoAsignatura resultado) =>
            PorInasistencia
                ? resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
                : resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia;
    }
}
