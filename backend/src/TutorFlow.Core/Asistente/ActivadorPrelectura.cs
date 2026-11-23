using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class ActivadorPrelectura
{
    private const string PruebaAcademica = " prueba academica ";

    private static readonly HashSet<string> Palabras = new(StringComparer.Ordinal)
    {
        "estudiante", "estudiantes", "programa", "programas", "perdida", "perdidas", "perdido", "perdidos", "perdio", "perdieron",
        "reprobo", "reprobaron", "reprobada", "reprobadas"
    };

    public static bool Activa(ContextoUsuario contexto, IReadOnlyList<string> palabras, IReadOnlyList<string> programas) =>
        contexto.Usuario.Rol == Rol.Coordinador
        && programas.Count > 0
        && (palabras.Any(Palabras.Contains)
            || $" {string.Join(' ', palabras)} ".Contains(PruebaAcademica, StringComparison.Ordinal)
            || MencionaEstudianteDeSusProgramas(contexto, palabras, programas));

    public static bool MencionaEstudianteDeSusProgramas(ContextoUsuario contexto, IReadOnlyList<string> palabras, IReadOnlyList<string> programas)
    {
        var propios = contexto.Directorio.Estudiantes.Where(estudiante => programas.Contains(estudiante.ProgramaId)).Select(estudiante => estudiante.Id).ToHashSet();
        return DetectorTerceros.PersonasMencionadas(string.Join(' ', palabras), contexto.Directorio).Any(propios.Contains);
    }
}
