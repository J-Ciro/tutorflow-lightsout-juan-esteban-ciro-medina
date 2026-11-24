namespace TutorFlow.Core.Autorizacion;

internal sealed record AmbitoPersonal(IReadOnlySet<string> ClavesDeId, IReadOnlySet<string> ClavesDeNombre)
{
    public static AmbitoPersonal De(Usuario usuario, DirectorioAcademico directorio) => usuario.Rol switch
    {
        Rol.Coordinador => DeCoordinador(usuario, directorio),
        _ => DeEstudiante(usuario, directorio)
    };

    public static string Clave(string idPersona) =>
        string.Concat(TokenizadorTexto.Valores(idPersona));

    public static IReadOnlyList<string> ProgramasDe(Usuario usuario, DirectorioAcademico directorio) => usuario.Rol switch
    {
        Rol.Coordinador => ProgramasDeCoordinador(usuario, directorio),
        _ => ProgramasDeEstudiante(usuario, directorio)
    };

    private static AmbitoPersonal DeEstudiante(Usuario usuario, DirectorioAcademico directorio)
    {
        var programas = ProgramasDeEstudiante(usuario, directorio);
        var coordinadores = directorio.Coordinadores
            .Where(coordinador => coordinador.ProgramasIds.Intersect(programas).Any())
            .Select(coordinador => Clave(coordinador.Id));
        var propia = Clave(usuario.Id);
        return new AmbitoPersonal(
            new HashSet<string> { propia },
            new HashSet<string>(coordinadores) { propia });
    }

    private static AmbitoPersonal DeCoordinador(Usuario usuario, DirectorioAcademico directorio)
    {
        var programas = ProgramasDeCoordinador(usuario, directorio);
        var claves = directorio.Estudiantes
            .Where(estudiante => programas.Contains(estudiante.ProgramaId))
            .Select(estudiante => Clave(estudiante.Id))
            .Append(Clave(usuario.Id))
            .ToHashSet();
        return new AmbitoPersonal(claves, claves);
    }

    private static List<string> ProgramasDeCoordinador(Usuario usuario, DirectorioAcademico directorio) =>
        directorio.Coordinadores
            .Where(coordinador => coordinador.Id == usuario.Id)
            .SelectMany(coordinador => coordinador.ProgramasIds)
            .Where(Identificadores.EsIdPrograma)
            .Distinct()
            .ToList();

    private static List<string> ProgramasDeEstudiante(Usuario usuario, DirectorioAcademico directorio) =>
        directorio.Estudiantes
            .Where(estudiante => estudiante.Id == usuario.Id)
            .Select(estudiante => estudiante.ProgramaId)
            .Where(Identificadores.EsIdPrograma)
            .ToList();
}
