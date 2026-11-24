namespace TutorFlow.Core.Autorizacion;

public sealed class PoliticaAcceso
{
    private readonly DecisionAcceso _decisionPermitida = DecisionAcceso.Permitir();

    public DecisionAcceso EvaluarIdentidad(Usuario usuario, DirectorioAcademico directorio) =>
        ExisteIdentidad(usuario, directorio)
            ? _decisionPermitida
            : DecisionAcceso.Denegar(MotivoDenegacion.IdentidadInvalida);

    public DecisionAcceso EvaluarPregunta(Usuario usuario, string pregunta, DirectorioAcademico directorio)
    {
        var texto = NormalizadorTexto.Normalizar(pregunta);
        var motivo = MotivoTercero(usuario, texto, directorio)
            ?? MotivoPrograma(usuario, texto, directorio)
            ?? MotivoAgregado(usuario, texto)
            ?? MotivoCapacidadAjena(usuario, texto, directorio);
        return motivo is { } denegado ? DecisionAcceso.Denegar(denegado) : DecisionAcceso.Permitir();
    }

    public DecisionAcceso EvaluarLecturaEstudiante(Usuario usuario, string estudianteId, DirectorioAcademico directorio) =>
        PuedeLeerEstudiante(usuario, estudianteId, directorio)
            ? DecisionAcceso.Permitir()
            : DecisionAcceso.Denegar(MotivoDenegacion.HerramientaIdAjeno);

    public IReadOnlyList<string> ProgramasAutorizados(Usuario usuario, DirectorioAcademico directorio) =>
        EvaluarIdentidad(usuario, directorio).Permitido ? AmbitoPersonal.ProgramasDe(usuario, directorio) : [];

    public IReadOnlyList<string> IdsPersonasAutorizadas(Usuario usuario, DirectorioAcademico directorio)
    {
        if (!EvaluarIdentidad(usuario, directorio).Permitido)
        {
            return [];
        }

        var ambito = AmbitoPersonal.De(usuario, directorio);
        return [.. directorio.Estudiantes.Select(e => e.Id)
            .Concat(directorio.Coordinadores.Select(c => c.Id))
            .Where(id => ambito.ClavesDeNombre.Contains(AmbitoPersonal.Clave(id)))];
    }

    private static bool ExisteIdentidad(Usuario usuario, DirectorioAcademico directorio) => usuario.Rol switch
    {
        Rol.Estudiante => ExisteEstudiante(usuario.Id, directorio),
        Rol.Coordinador => ExisteCoordinador(usuario.Id, directorio),
        _ => false
    };

    private static bool ExisteEstudiante(string id, DirectorioAcademico directorio) =>
        Identificadores.EsIdEstudiante(id) && directorio.Estudiantes.Any(e => e.Id == id);

    private static bool ExisteCoordinador(string id, DirectorioAcademico directorio) =>
        Identificadores.EsIdCoordinador(id) && directorio.Coordinadores.Any(c => c.Id == id);

    private bool PuedeLeerEstudiante(Usuario usuario, string estudianteId, DirectorioAcademico directorio)
    {
        var estudiante = directorio.Estudiantes.FirstOrDefault(e => e.Id == estudianteId);
        if (estudiante is null || !Identificadores.EsIdEstudiante(estudianteId))
        {
            return false;
        }

        return usuario.Rol == Rol.Estudiante
            ? usuario.Id == estudianteId
            : ProgramasAutorizados(usuario, directorio).Contains(estudiante.ProgramaId);
    }

    private static MotivoDenegacion? MotivoTercero(Usuario usuario, string texto, DirectorioAcademico directorio) =>
        DetectorTerceros.MencionaTercero(usuario, texto, directorio) ? MotivoDenegacion.TerceroMencionado : null;

    private MotivoDenegacion? MotivoPrograma(Usuario usuario, string texto, DirectorioAcademico directorio) =>
        usuario.Rol == Rol.Coordinador && DetectorProgramas.MencionaProgramaAjeno(texto, ProgramasAutorizados(usuario, directorio), directorio)
            ? MotivoDenegacion.ProgramaAjeno
            : null;

    private static MotivoDenegacion? MotivoAgregado(Usuario usuario, string texto) =>
        usuario.Rol == Rol.Estudiante && DetectorAgregados.PideAgregados(texto) ? MotivoDenegacion.Agregado : null;

    private static MotivoDenegacion? MotivoCapacidadAjena(Usuario usuario, string texto, DirectorioAcademico directorio) =>
        DetectorCapacidadAjena.PideCapacidadAjena(usuario, texto, directorio) ? MotivoDenegacion.CapacidadAjena : null;
}
