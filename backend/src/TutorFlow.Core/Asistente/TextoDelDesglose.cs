using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal static class TextoDelDesglose
{
    private const string TotalSinRegistro = "Total: sin registro";

    public static string? De(ResumenEstudiante resumen, IReadOnlyCollection<string> claves, bool terceraPersona)
    {
        var elegidas = resumen.Asignaturas
            .Where(resultado => claves.Count == 0 || claves.Contains(AmbitoPersonal.Clave(resultado.Asignatura.Codigo)))
            .ToList();
        return elegidas.Count == 0
            ? null
            : string.Join("\n", elegidas.Select(resultado => FormateadorDatos.LineaDeAsistencia(resultado, terceraPersona)).Append(Total(elegidas)));
    }

    private static string Total(IReadOnlyList<ResultadoAsignatura> elegidas)
    {
        var conRegistro = elegidas.Where(resultado => resultado.Asignatura.Asistencia.Inasistencias is not null).ToList();
        var sinRegistro = elegidas.Except(conRegistro).Select(resultado => resultado.Asignatura.Nombre).ToList();
        var faltas = conRegistro.Sum(resultado => resultado.Asignatura.Asistencia.Inasistencias.GetValueOrDefault());
        return conRegistro.Count == 0 ? TotalSinRegistro : $"Total: {faltas} faltas{Complemento(sinRegistro)}";
    }

    private static string Complemento(List<string> sinRegistro) =>
        sinRegistro.Count == 0 ? string.Empty : $" (sin registro: {string.Join(", ", sinRegistro)})";
}
