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

    public static bool Activa(Rol rol, string pregunta)
    {
        if (rol != Rol.Estudiante)
        {
            return false;
        }

        var palabras = TokenizadorTexto.Valores(pregunta);
        return palabras.Any(MarcadoresDePrimeraPersona.Contains) && PideDatosAcademicos(palabras);
    }

    private static bool PideDatosAcademicos(IReadOnlyList<string> palabras) =>
        palabras.Any(TerminosDeDatosAcademicos.Contains)
        || $" {string.Join(' ', palabras)} ".Contains(PruebaAcademica, StringComparison.Ordinal);
}
