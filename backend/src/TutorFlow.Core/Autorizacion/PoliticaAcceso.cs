namespace TutorFlow.Core.Autorizacion;

public sealed class PoliticaAcceso
{
    private static readonly DecisionAcceso AccesoPermitido = DecisionAcceso.Permitir();
    private readonly DecisionAcceso _decisionPermitida = AccesoPermitido;

    public DecisionAcceso EvaluarIdentidad(Usuario usuario, DirectorioAcademico directorio) =>
        ExisteIdentidad(usuario, directorio)
            ? _decisionPermitida
            : DecisionAcceso.Denegar(MotivoDenegacion.IdentidadInvalida);

    public DecisionAcceso EvaluarPregunta(Usuario usuario, string pregunta, DirectorioAcademico directorio)
    {
        var texto = NormalizadorTexto.Normalizar(pregunta);
        var motivo = MotivoTercero(usuario, texto, directorio)
            ?? MotivoPrograma(usuario, texto, directorio)
            ?? MotivoAgregado(usuario, texto);
        return motivo is { } denegado ? DecisionAcceso.Denegar(denegado) : DecisionAcceso.Permitir();
    }

    public DecisionAcceso EvaluarLecturaEstudiante(Usuario usuario, string estudianteId, DirectorioAcademico directorio) =>
        PuedeLeerEstudiante(usuario, estudianteId, directorio)
            ? DecisionAcceso.Permitir()
            : DecisionAcceso.Denegar(MotivoDenegacion.HerramientaIdAjeno);

    public IReadOnlyList<string> ProgramasAutorizados(Usuario usuario, DirectorioAcademico directorio) =>
        EvaluarIdentidad(usuario, directorio).Permitido ? AmbitoPersonal.ProgramasDe(usuario, directorio) : [];

    private static bool ExisteIdentidad(Usuario usuario, DirectorioAcademico directorio) => usuario.Rol switch
    {
        Rol.Estudiante => Identificadores.EsIdEstudiante(usuario.Id) && directorio.Estudiantes.Any(e => e.Id == usuario.Id),
        Rol.Coordinador => Identificadores.EsIdCoordinador(usuario.Id) && directorio.Coordinadores.Any(c => c.Id == usuario.Id),
        _ => false
    };

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
}
