using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed class AfirmacionesDeEstado
{
    private const int PalabrasHastaElVerbo = 2;
    private const string Motivo = "la afirmación de estado no coincide con los datos entregados";

    private static readonly HashSet<string> VerbosDeEstar = new(StringComparer.Ordinal) { "esta", "estas", "estan" };
    private static readonly HashSet<string> PalabrasQueAnulan = new(StringComparer.Ordinal) { "no", "ni", "sin" };
    private static readonly string[] EnPruebaAcademica = ["en", "prueba", "academica"];

    private readonly IReadOnlyList<AsignaturaCatalogo> _catalogo;

    public AfirmacionesDeEstado(IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        _catalogo = catalogo;
    }

    public string? Motivos(IReadOnlyList<BloqueDeTexto> bloques, ContextoDeAfirmaciones contexto)
    {
        var catalogo = CatalogoAsignaturas.Vigente(_catalogo, contexto.Directorio);
        return bloques.Select(bloque => MotivoDelBloque(bloque, catalogo, contexto)).FirstOrDefault(motivo => motivo is not null);
    }

    private static string? MotivoDelBloque(BloqueDeTexto bloque, IReadOnlyList<AsignaturaCatalogo> catalogo, ContextoDeAfirmaciones contexto)
    {
        var personas = contexto.PersonasDe(bloque);
        if (personas.Ambiguo)
        {
            return Exigencias(bloque.Texto, TokenizadorTexto.Tokenizar(bloque.Texto), catalogo).Count > 0 ? Motivo : null;
        }

        return personas.Tramos.Any(tramo => Exigencias(bloque.Texto, tramo.Tokens, catalogo).Any(exigencia => !contexto.Cumple(tramo.PersonaId, exigencia))) ? Motivo : null;
    }

    private static List<Func<ResumenEstudiante, bool>> Exigencias(string texto, IReadOnlyList<TokenTexto> tokens, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        var palabras = tokens.Select(token => token.Valor).ToList();
        return [.. DeAprobacion(tokens, palabras, catalogo), .. DePruebaAcademica(texto, tokens)];
    }

    private static IEnumerable<Func<ResumenEstudiante, bool>> DeAprobacion(
        IReadOnlyList<TokenTexto> tokens,
        IReadOnlyList<string> palabras,
        IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        var formas = FormasDePerdida.Aprobaciones(palabras);
        return formas.Count == 0
            ? []
            : CatalogoAsignaturas.PrimerasApariciones(tokens, catalogo)
                .Where(nombrada => !FormasDePerdida.MasCercana(formas, nombrada).Negada)
                .Select(nombrada => EstaAprobada(nombrada.Clave));
    }

    private static Func<ResumenEstudiante, bool> EstaAprobada(string clave) =>
        resumen => resumen.Asignaturas.Any(resultado =>
            AmbitoPersonal.Clave(resultado.Asignatura.Codigo) == clave && resultado.Estado == EstadoAsignatura.Aprobada);

    private static IEnumerable<Func<ResumenEstudiante, bool>> DePruebaAcademica(string texto, IReadOnlyList<TokenTexto> tokens)
    {
        var palabras = tokens.Select(token => token.Valor).ToList();
        return Enumerable.Range(0, palabras.Count)
            .Where(indice => VerbosDeEstar.Contains(palabras[indice]) && SigueEnPruebaAcademica(palabras, indice) && !Anulada(texto, tokens, indice))
            .Select(_ => (Func<ResumenEstudiante, bool>)(resumen => resumen.PruebaAcademica));
    }

    private static bool SigueEnPruebaAcademica(List<string> palabras, int indice) =>
        indice + EnPruebaAcademica.Length < palabras.Count
        && EnPruebaAcademica.Select((palabra, desplazamiento) => palabras[indice + 1 + desplazamiento] == palabra).All(coincide => coincide);

    private static bool Anulada(string texto, IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice > 0 && (PalabrasQueAnulan.Contains(tokens[indice - 1].Valor) || SiSinTilde(texto, tokens[indice - 1]) || ConCuandoCerca(tokens, indice) || EstudianteQue(tokens, indice));

    private static bool SiSinTilde(string texto, TokenTexto anterior) =>
        anterior.Valor == "si" && string.Equals(texto[anterior.Inicio..anterior.Fin], "si", StringComparison.OrdinalIgnoreCase);

    private static bool ConCuandoCerca(IReadOnlyList<TokenTexto> tokens, int indice) =>
        Enumerable.Range(Math.Max(0, indice - PalabrasHastaElVerbo - 1), PalabrasHastaElVerbo + 1).Any(posicion => tokens[posicion].Valor == "cuando");

    private static bool EstudianteQue(IReadOnlyList<TokenTexto> tokens, int indice) =>
        indice > 1 && tokens[indice - 1].Valor == "que" && tokens[indice - 2].Valor is "estudiante" or "estudiantes";
}
