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
            || ((frases.Contiene(Capacidad(usuario)) || TenenciaSobrePersona(frases)) && PideContenido(frases)));

    private static bool TenenciaSobrePersona(FrasesDelTexto frases) =>
        frases.Ocurrencias(VocabularioCapacidadAjena.CapacidadDeTenencia).Any(ocurrencia => SobrePersona(frases.PalabrasDesde(ocurrencia.Fin)));

    private static bool SobrePersona(IReadOnlyList<string> resto) =>
        !(resto.Count > 0 && resto[0] == VocabularioCapacidadAjena.ConjuncionQue)
        && TemaEsPersona(resto.SkipWhile(palabra => !VocabularioCapacidadAjena.PreposicionesDeTema.Contains(palabra)).Skip(1).FirstOrDefault());

    private static bool TemaEsPersona(string? tema) =>
        tema is null || VocabularioCapacidadAjena.PalabrasDePersona.Contains(tema);

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

    private static string[] Capacidad(Usuario usuario) =>
        usuario.Rol == Rol.Coordinador
            ? [.. VocabularioCapacidadAjena.Capacidad, .. VocabularioCapacidadAjena.CapacidadDelCoordinador]
            : VocabularioCapacidadAjena.Capacidad;

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
