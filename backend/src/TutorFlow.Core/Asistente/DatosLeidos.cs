using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal static class DatosLeidos
{
    public static DatoConsultado SinAsignatura(string estudianteId, string programaId, RecursoConsultado recurso) =>
        new(estudianteId, programaId, recurso);

    public static IEnumerable<DatoConsultado> PorAsignatura(
        ResumenEstudiante resumen,
        string programaId,
        params RecursoConsultado[] recursos) =>
        PorCodigos(resumen.EstudianteId, programaId, resumen.Asignaturas.Select(r => r.Asignatura.Codigo), recursos);

    public static IEnumerable<DatoConsultado> PorCodigos(
        string estudianteId,
        string programaId,
        IEnumerable<string> codigos,
        params RecursoConsultado[] recursos) =>
        recursos.SelectMany(recurso => codigos.Select(codigo => new DatoConsultado(estudianteId, programaId, recurso, codigo)));
}
