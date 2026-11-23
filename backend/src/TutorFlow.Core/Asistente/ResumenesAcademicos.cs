using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal static class ResumenesAcademicos
{
    public static async Task<ResumenEstudiante> ResumirAsync(IApiAcademica api, string estudianteId, CancellationToken cancellationToken) =>
        await ResumirAsync(api, estudianteId, await api.ObtenerNotasAsync(estudianteId, cancellationToken), cancellationToken);

    public static async Task<ResumenEstudiante> ResumirAsync(
        IApiAcademica api,
        string estudianteId,
        IReadOnlyList<NotasAsignatura> notas,
        CancellationToken cancellationToken)
    {
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
