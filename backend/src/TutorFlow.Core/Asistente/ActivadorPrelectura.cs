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

    private static readonly string[] FrasesNormativas =
    [
        " que dice ", " segun el reglamento ", " que significa ", " que pasa si ", " un estudiante ", " el estudiante ",
        " cada estudiante ", " una asignatura ", " una materia "
    ];

    private static readonly HashSet<string> Posesivos = new(StringComparer.Ordinal)
    {
        "mi", "mis", "nuestro", "nuestros", "nuestra", "nuestras"
    };

    public static bool Activa(ContextoUsuario contexto, IReadOnlyList<string> palabras, IReadOnlyList<string> programas) =>
        contexto.Usuario.Rol == Rol.Coordinador
        && programas.Count > 0
        && (MencionaEstudianteDeSusProgramas(contexto, palabras, programas) || ActivaPorPalabras(palabras));

    public static bool MencionaEstudianteDeSusProgramas(ContextoUsuario contexto, IReadOnlyList<string> palabras, IReadOnlyList<string> programas)
    {
        var propios = contexto.Directorio.Estudiantes.Where(estudiante => programas.Contains(estudiante.ProgramaId)).Select(estudiante => estudiante.Id).ToHashSet();
        return DetectorTerceros.PersonasMencionadas(string.Join(' ', palabras), contexto.Directorio).Any(propios.Contains);
    }

    private static bool ActivaPorPalabras(IReadOnlyList<string> palabras)
    {
        var texto = $" {string.Join(' ', palabras)} ";
        var hayActivador = palabras.Any(Palabras.Contains) || texto.Contains(PruebaAcademica, StringComparison.Ordinal);
        return hayActivador && (!EsNormativaGenerica(texto) || palabras.Any(Posesivos.Contains));
    }

    private static bool EsNormativaGenerica(string texto) =>
        FrasesNormativas.Any(frase => texto.Contains(frase, StringComparison.Ordinal));
}
