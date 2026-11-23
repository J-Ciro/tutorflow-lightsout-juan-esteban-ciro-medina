using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

public static class ActivadorResumenPropio
{
    private const string PruebaAcademica = " prueba academica ";

    private static readonly HashSet<string> MarcadoresDePrimeraPersona = new(StringComparer.Ordinal)
    {
        "mi", "mis", "tengo", "llevo", "voy", "cumplo", "soy", "estoy", "perdi", "gane", "saque", "quede"
    };

    private static readonly HashSet<string> TerminosDeDatosAcademicos = new(StringComparer.Ordinal)
    {
        "nota", "notas", "definitiva", "promedio", "corte", "falta", "faltas", "asistencia", "inasistencia",
        "inasistencias", "beca", "habilitar", "materia", "materias", "asignatura", "asignaturas", "perdida",
        "perdidas", "aprobe"
    };

    public static bool Activa(ContextoUsuario contexto, string pregunta)
    {
        if (contexto.Usuario.Rol != Rol.Estudiante)
        {
            return false;
        }

        var palabras = TokenizadorTexto.Valores(pregunta);
        return PideDatosAcademicos(palabras) && (palabras.Any(MarcadoresDePrimeraPersona.Contains) || MencionaSuNombre(contexto, palabras));
    }

    internal static bool PideDatosAcademicos(IReadOnlyList<string> palabras) =>
        palabras.Any(TerminosDeDatosAcademicos.Contains)
        || $" {string.Join(' ', palabras)} ".Contains(PruebaAcademica, StringComparison.Ordinal);

    private static bool MencionaSuNombre(ContextoUsuario contexto, IReadOnlyList<string> palabras) =>
        DetectorTerceros.MencionaNombreCompleto(contexto.Usuario.Id, string.Join(' ', palabras), contexto.Directorio);
}
