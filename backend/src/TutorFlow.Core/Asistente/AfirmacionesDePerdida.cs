using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class AfirmacionesDePerdida
{
    private const string MotivoSinPersonaNiAsignatura = "la afirmación de pérdida no coincide con los datos entregados";

    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public AfirmacionesDePerdida(IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _catalogo = catalogo;
    }

    public string? Motivo(
        IReadOnlyList<BloqueDeTexto> bloques,
        Usuario usuario,
        IReadOnlyCollection<string> idsAutorizados,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        var catalogo = CatalogoAsignaturas.Vigente(_catalogo, directorio);
        return bloques
            .Select(bloque => MotivoDelBloque(bloque, TramosDePersonas.De(bloque, usuario, idsAutorizados, directorio), catalogo, usuario, directorio, resumenesLeidos))
            .FirstOrDefault(motivo => motivo is not null);
    }

    private static string? MotivoDelBloque(
        BloqueDeTexto bloque,
        PersonasDeBloque personas,
        IReadOnlyList<AsignaturaCatalogo> catalogo,
        Usuario usuario,
        DirectorioAcademico directorio,
        IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos)
    {
        if (personas.Ambiguo)
        {
            return Exigencias(bloque, TokenizadorTexto.Tokenizar(bloque.Texto), catalogo, _ => false).Count > 0 ? MotivoSinPersonaNiAsignatura : null;
        }

        return personas.Tramos
            .SelectMany(tramo => Exigencias(bloque, tramo.Tokens, catalogo, new OracionGenerica(bloque, tramo.Tokens, usuario, directorio).Es).Where(exigencia => !Cumple(exigencia, tramo.PersonaId, resumenesLeidos)).Select(exigencia => MotivoDe(exigencia, tramo.PersonaId, usuario, directorio)))
            .FirstOrDefault();
    }

    private static bool Cumple(ExigenciaDePerdida exigencia, string personaId, IReadOnlyDictionary<string, ResumenEstudiante> resumenesLeidos) =>
        resumenesLeidos.TryGetValue(personaId, out var resumen) && exigencia.Cumple(resumen);

    private static string MotivoDe(ExigenciaDePerdida exigencia, string personaId, Usuario usuario, DirectorioAcademico directorio)
    {
        var persona = usuario.Rol == Rol.Coordinador ? $" de {NombreDe(personaId, directorio)}" : string.Empty;
        var asignatura = exigencia.Escrita is null ? string.Empty : $" en {exigencia.Escrita}";
        return $"la afirmación de pérdida{persona}{asignatura} no coincide con los datos entregados";
    }

    private static string NombreDe(string personaId, DirectorioAcademico directorio) =>
        directorio.Estudiantes.Select(estudiante => (estudiante.Id, estudiante.Nombre))
            .Concat(directorio.Coordinadores.Select(coordinador => (coordinador.Id, coordinador.Nombre)))
            .FirstOrDefault(persona => persona.Id == personaId).Nombre ?? personaId;

    private static List<ExigenciaDePerdida> Exigencias(BloqueDeTexto bloque, IReadOnlyList<TokenTexto> tokens, IReadOnlyList<AsignaturaCatalogo> catalogo, Func<FormaDePerdida, bool> esGenerica)
    {
        var formas = FormasDePerdida.En([.. tokens.Select(token => token.Valor)]);
        if (formas.Count == 0)
        {
            return [];
        }

        var nombradas = CatalogoAsignaturas.PrimerasApariciones(tokens, catalogo);
        var asociadas = nombradas.Select(nombrada => (Nombrada: nombrada, Forma: FormasDePerdida.MasCercana(formas, nombrada))).ToList();
        var porAsignatura = asociadas
            .Where(asociada => !asociada.Forma.Negada)
            .Select(asociada => new ExigenciaDePerdida(asociada.Nombrada.Clave, PorInasistencia(tokens, nombradas, asociada.Nombrada), Escrita(bloque, tokens, asociada.Nombrada)));
        var sinAsignatura = formas
            .Where(forma => forma.Nominal && !forma.Negada && !esGenerica(forma) && asociadas.All(asociada => asociada.Forma != forma))
            .Select(forma => new ExigenciaDePerdida(null, forma.PorInasistencia, null));
        return [.. porAsignatura.Concat(sinAsignatura)];
    }

    private static string Escrita(BloqueDeTexto bloque, IReadOnlyList<TokenTexto> tokens, AparicionDeAsignatura aparicion) =>
        bloque.Texto[tokens[aparicion.Inicio].Inicio..tokens[aparicion.Fin].Fin];

    private static bool PorInasistencia(
        IReadOnlyList<TokenTexto> tokens,
        List<AparicionDeAsignatura> nombradas,
        AparicionDeAsignatura asignatura)
    {
        var inasistencias = Enumerable.Range(0, tokens.Count).Where(indice => FormasDePerdida.Inasistencias.Contains(tokens[indice].Valor)).ToList();
        var siguiente = nombradas.Where(nombrada => nombrada.Inicio > asignatura.Inicio).Select(nombrada => nombrada.Inicio).DefaultIfEmpty(int.MaxValue).Min();
        return inasistencias.Any(indice => indice < nombradas[0].Inicio || (indice >= asignatura.Inicio && indice < siguiente));
    }

    private sealed record ExigenciaDePerdida(string? Clave, bool PorInasistencia, string? Escrita)
    {
        public bool Cumple(ResumenEstudiante resumen) =>
            resumen.Asignaturas.Any(resultado => (Clave is null || AmbitoPersonal.Clave(resultado.Asignatura.Codigo) == Clave) && Satisface(resultado));

        private bool Satisface(ResultadoAsignatura resultado) =>
            PorInasistencia
                ? resultado.Estado == EstadoAsignatura.PerdidaPorInasistencia
                : resultado.Estado is EstadoAsignatura.Perdida or EstadoAsignatura.PerdidaPorInasistencia;
    }
}
