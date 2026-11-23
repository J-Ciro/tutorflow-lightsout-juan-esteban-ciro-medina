using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal static class ResumenesAcademicos
{
    public static async Task<ResumenEstudiante> ResumirAsync(IApiAcademica api, string estudianteId, CancellationToken cancellationToken)
    {
        var notas = await api.ObtenerNotasAsync(estudianteId, cancellationToken);
        var asistencia = await api.ObtenerAsistenciaAsync(estudianteId, cancellationToken);
        var asignaturas = notas.Select(nota => new Asignatura(
            nota.Codigo,
            nota.Nombre,
            nota.Creditos,
            nota.Cortes,
            asistencia.FirstOrDefault(a => a.Codigo == nota.Codigo)?.Asistencia ?? new Asistencia(0, null))).ToList();
        return CalculosAcademicos.ResumirEstudiante(estudianteId, asignaturas);
    }
}
