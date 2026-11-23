namespace TutorFlow.Core.Autorizacion;

public static class DetectorCapacidadAjena
{
    public static bool PideCapacidadAjena(Usuario usuario, string textoNormalizado, DirectorioAcademico directorio)
    {
        var frases = new FrasesDelTexto(textoNormalizado);
        return CumpleActorYCapacidad(usuario, frases, directorio)
            || CumpleEstudianteConPermisos(usuario, textoNormalizado, frases, directorio);
    }

    private static bool CumpleActorYCapacidad(Usuario usuario, FrasesDelTexto frases, DirectorioAcademico directorio) =>
        frases.Contiene(Actores(usuario, directorio))
        && (frases.Contiene(VocabularioCapacidadAjena.PermisosYSimulacion)
            || (frases.Contiene(Capacidad(usuario)) && PideContenido(frases)));

    private static bool CumpleEstudianteConPermisos(Usuario usuario, string texto, FrasesDelTexto frases, DirectorioAcademico directorio) =>
        usuario.Rol == Rol.Coordinador
        && frases.Contiene(VocabularioCapacidadAjena.PermisosYSimulacion.Except(VocabularioCapacidadAjena.AlPreguntar))
        && (frases.Contiene(VocabularioCapacidadAjena.EstudiantesGenericos) || MencionaEstudiantePropio(usuario, texto, directorio));

    private static bool PideContenido(FrasesDelTexto frases) =>
        frases.ContieneSeguidaDe(VocabularioCapacidadAjena.IntroductoresDeContenido, SeguidoresDeContenido());

    private static string[] SeguidoresDeContenido() =>
    [
        .. VocabularioCapacidadAjena.Capacidad,
        .. VocabularioCapacidadAjena.ModalesQueSiguen,
        .. VocabularioCapacidadAjena.ContenidoQueSigue
    ];

    private static IEnumerable<string> Capacidad(Usuario usuario) =>
        usuario.Rol == Rol.Coordinador
            ? [.. VocabularioCapacidadAjena.Capacidad, .. VocabularioCapacidadAjena.CapacidadDeTenencia, .. VocabularioCapacidadAjena.CapacidadDelCoordinador]
            : [.. VocabularioCapacidadAjena.Capacidad, .. VocabularioCapacidadAjena.CapacidadDeTenencia];

    private static IEnumerable<string> Actores(Usuario usuario, DirectorioAcademico directorio) =>
    [
        .. usuario.Rol == Rol.Coordinador ? VocabularioCapacidadAjena.ActoresDelCoordinador : VocabularioCapacidadAjena.ActoresDelEstudiante,
        .. VocabularioCapacidadAjena.ActoresDeRol,
        .. PalabrasDeNombresDeCoordinadores(usuario, directorio)
    ];

    private static IEnumerable<string> PalabrasDeNombresDeCoordinadores(Usuario usuario, DirectorioAcademico directorio) =>
        directorio.Coordinadores
            .Where(coordinador => coordinador.Id != usuario.Id)
            .SelectMany(coordinador => TokenizadorTexto.Valores(coordinador.Nombre));

    private static bool MencionaEstudiantePropio(Usuario usuario, string texto, DirectorioAcademico directorio)
    {
        var programas = AmbitoPersonal.ProgramasDe(usuario, directorio);
        var propios = directorio.Estudiantes.Where(e => programas.Contains(e.ProgramaId)).Select(e => e.Id).ToHashSet();
        return DetectorTerceros.PersonasMencionadas(texto, directorio).Any(propios.Contains);
    }
}
