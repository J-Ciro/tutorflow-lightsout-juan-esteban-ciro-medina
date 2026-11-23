using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class AfirmacionesDePerdida
{
    private const int PalabrasDeNegacion = 2;

    private static readonly HashSet<string> FormasDePerdida = new(StringComparer.Ordinal)
    {
        "perdi", "perdio", "perdiste", "perdieron", "perdido", "perdidos", "perdida", "perdidas"
    };

    private static readonly HashSet<string> Negaciones = new(StringComparer.Ordinal)
    {
        "no", "ni", "sin", "ningun", "ninguna"
    };

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
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos) =>
        TramosDePersonas.Oraciones(respuesta)
            .SelectMany(oracion => TramosDePersonas.De(oracion, idsAutorizados, directorio))
            .All(tramo => TramoEsVerificable(tramo, resumenesLeidos));

    private bool TramoEsVerificable(TramoDePersona tramo, IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        var nombradas = AsignaturasNombradas(tramo.Tokens);
        if (nombradas.Count == 0 || !HayPerdidaNoNegada(tramo.Tokens))
        {
            return true;
        }

        return resumenesLeidos.TryGetValue(tramo.PersonaId, out var resumen)
            && ExigenciasPorAsignatura(tramo.Tokens, nombradas).All(exigencia => Cumple(exigencia, resumen));
    }

    private static bool HayPerdidaNoNegada(IReadOnlyList<TokenTexto> tokens) =>
        Enumerable.Range(0, tokens.Count).Any(indice => FormasDePerdida.Contains(tokens[indice].Valor) && !EstaNegada(tokens, indice));

    private static bool EstaNegada(IReadOnlyList<TokenTexto> tokens, int indice) =>
        Enumerable.Range(Math.Max(0, indice - PalabrasDeNegacion), Math.Min(PalabrasDeNegacion, indice))
            .Any(anterior => Negaciones.Contains(tokens[anterior].Valor));

    private List<AsignaturaNombrada> AsignaturasNombradas(IReadOnlyList<TokenTexto> tokens) =>
        [.. _catalogo
            .Select(asignatura => new AsignaturaNombrada(AmbitoPersonal.Clave(asignatura.Codigo), PrimeraAparicion(tokens, asignatura)))
            .Where(nombrada => nombrada.Indice >= 0)
            .OrderBy(nombrada => nombrada.Indice)];

    private static int PrimeraAparicion(IReadOnlyList<TokenTexto> tokens, AsignaturaCatalogo asignatura)
    {
        var apariciones = new[] { asignatura.Nombre, asignatura.Codigo }
            .Select(texto => TokenizadorTexto.BuscarSecuencia(tokens, TokenizadorTexto.Valores(texto)))
            .Where(indice => indice >= 0)
            .ToList();
        return apariciones.Count == 0 ? -1 : apariciones.Min();
    }

    private static IEnumerable<ExigenciaDeAsignatura> ExigenciasPorAsignatura(
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<AsignaturaNombrada> nombradas)
    {
        var inasistencias = Enumerable.Range(0, tokens.Count).Where(indice => Inasistencias.Contains(tokens[indice].Valor)).ToList();
        var antesDeLaPrimera = inasistencias.Any(indice => indice < nombradas[0].Indice);
        return nombradas.Select((nombrada, posicion) =>
        {
            var hasta = posicion + 1 < nombradas.Count ? nombradas[posicion + 1].Indice : int.MaxValue;
            var porInasistencia = antesDeLaPrimera || inasistencias.Any(indice => indice >= nombrada.Indice && indice < hasta);
            return new ExigenciaDeAsignatura(nombrada.Clave, porInasistencia);
        });
    }

    private static bool Cumple(ExigenciaDeAsignatura exigencia, ResumenEstudiante resumen)
    {
        var resultado = resumen.Asignaturas.FirstOrDefault(r => AmbitoPersonal.Clave(r.Asignatura.Codigo) == exigencia.Clave);
        return resultado is not null
            && (exigencia.PorInasistencia
                ? resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
                : resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia);
    }

    private sealed record AsignaturaNombrada(string Clave, int Indice);

    private sealed record ExigenciaDeAsignatura(string Clave, bool PorInasistencia);
}
