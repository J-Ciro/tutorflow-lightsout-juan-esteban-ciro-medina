using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class ActivadorResumenPropio
{
    private const string PalabraPuedo = "puedo";
    private const string PruebaAcademica = " prueba academica ";

    private static readonly string[] GenericosIndefinidos =
    [
        " una materia ", " una asignatura ", " una nota ", " un estudiante "
    ];

    private static readonly HashSet<string> MarcadoresDePrimeraPersona = new(StringComparer.Ordinal)
    {
        "mi", "mis", "tengo", "llevo", "voy", "cumplo", "soy", "estoy", "perdi", "gane", "saque", "quede"
    };

    private static readonly HashSet<string> TerminosGenericos = new(StringComparer.Ordinal)
    {
        "materia", "materias", "asignatura", "asignaturas"
    };

    private static readonly HashSet<string> TerminosDeDatosAcademicos =
    [
        "nota", "notas", "definitiva", "promedio", "corte", "falta", "faltas", "asistencia", .. FormasDePerdida.Inasistencias,
        "beca", "habilitar", "materia", "materias", "asignatura", "asignaturas", "perdida",
        "perdidas", "aprobe"
    ];

    public static ActivacionResumen Evaluar(ContextoUsuario contexto, string pregunta, IReadOnlyList<AsignaturaCatalogo> catalogo)
    {
        if (contexto.Usuario.Rol != Rol.Estudiante)
        {
            return ActivacionResumen.Ninguna;
        }

        var palabras = TokenizadorTexto.Valores(pregunta);
        if (PrimeraPersona(contexto, palabras) || PuedoConTerminoEspecifico(palabras))
        {
            return new ActivacionResumen(true, []);
        }

        return new ActivacionResumen(false, CatalogoAsignaturas.CodigosNombrados(pregunta, catalogo, contexto.Directorio.CodigosAsignatura));
    }

    internal static bool PideDatosAcademicos(IReadOnlyList<string> palabras) =>
        palabras.Any(TerminosDeDatosAcademicos.Contains) || Contiene(palabras, PruebaAcademica);

    private static bool PrimeraPersona(ContextoUsuario contexto, IReadOnlyList<string> palabras) =>
        PideDatosAcademicos(palabras)
        && (palabras.Any(MarcadoresDePrimeraPersona.Contains) || MencionaSuNombre(contexto, palabras));

    private static bool PuedoConTerminoEspecifico(IReadOnlyList<string> palabras) =>
        palabras.Contains(PalabraPuedo)
        && TieneTerminoEspecifico(palabras)
        && !GenericosIndefinidos.Any(frase => Contiene(palabras, frase));

    private static bool TieneTerminoEspecifico(IReadOnlyList<string> palabras) =>
        palabras.Any(palabra => TerminosDeDatosAcademicos.Contains(palabra) && !TerminosGenericos.Contains(palabra))
        || Contiene(palabras, PruebaAcademica);

    private static bool Contiene(IReadOnlyList<string> palabras, string frase) =>
        $" {string.Join(' ', palabras)} ".Contains(frase, StringComparison.Ordinal);

    private static bool MencionaSuNombre(ContextoUsuario contexto, IReadOnlyList<string> palabras) =>
        DetectorTerceros.MencionaNombreCompleto(contexto.Usuario.Id, string.Join(' ', palabras), contexto.Directorio);
}
