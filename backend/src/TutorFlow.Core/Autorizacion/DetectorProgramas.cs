namespace TutorFlow.Core.Autorizacion;

public static class DetectorProgramas
{
    private static readonly HashSet<string> PalabrasSinPeso = new(StringComparer.Ordinal) { "de", "del", "la", "el", "y", "en", "para" };

    private static readonly HashSet<string> PalabrasDeDatos = new(StringComparer.Ordinal)
    {
        "estudiante", "estudiantes", "alumno", "alumnos", "promedio", "promedios", "nota", "notas", "definitiva",
        "definitivas", "perdieron", "perdio", "aprobaron", "reprobaron", "inasistencia", "inasistencias", "faltas",
        "cuantos", "cuantas", "lista"
    };

    public static bool MencionaProgramaAjeno(string textoNormalizado, IReadOnlyCollection<string> programasPropios, DirectorioAcademico directorio)
    {
        var tokens = TokenizadorTexto.Tokenizar(textoNormalizado);
        var esPreguntaDeDatos = tokens.Any(token => PalabrasDeDatos.Contains(token.Valor));
        return directorio.Programas
            .Where(programa => !programasPropios.Contains(programa.Id))
            .Any(programa => Menciona(tokens, programa.Id)
                || Menciona(tokens, programa.Nombre)
                || (esPreguntaDeDatos && MencionaTerminoDistintivo(tokens, programa.Nombre)));
    }

    private static bool Menciona(IReadOnlyList<TokenTexto> tokens, string texto) =>
        TokenizadorTexto.BuscarSecuencia(tokens, TokenizadorTexto.Valores(texto)) >= 0;

    private static bool MencionaTerminoDistintivo(IReadOnlyList<TokenTexto> tokens, string nombre) =>
        TokenizadorTexto.Valores(nombre).FirstOrDefault(palabra => !PalabrasSinPeso.Contains(palabra)) is { } termino
        && tokens.Any(token => token.Valor == termino);
}
