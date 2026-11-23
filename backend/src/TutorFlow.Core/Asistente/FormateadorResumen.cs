using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal static class FormateadorResumen
{
    public static string Resumir(ResumenEstudiante resumen)
    {
        var lineas = resumen.Asignaturas.Select(LineaDeAsignatura).ToList();
        lineas.Add($"Promedio ponderado del periodo: {FormateadorDatos.Promedio(resumen.Promedio)}.");
        lineas.Add($"Créditos matriculados: {resumen.CreditosMatriculados}.");
        lineas.Add($"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}.");
        lineas.Add($"Beca por rendimiento: {FormateadorDatos.TextoBeca(resumen.Beca)}.");
        lineas.Add($"Reglas aplicadas a estos datos: {ReglasDe(resumen)}.");
        return string.Join("\n", lineas);
    }

    private static string LineaDeAsignatura(ResultadoAsignatura resultado)
    {
        var asignatura = resultado.Asignatura;
        return $"{asignatura.Nombre} ({asignatura.Codigo}, {asignatura.Creditos} créditos): "
            + $"{FormateadorDatos.DescribirEstado(resultado)}; asistencia: {FormateadorDatos.DescribirAsistencia(resultado)}.";
    }

    private static string ReglasDe(ResumenEstudiante resumen)
    {
        IReadOnlyList<ResumenEstudiante> resumenes = [resumen];
        var reglas = ReglasAplicadas.DeNotas(resumenes).Concat(ReglasAplicadas.DeAsistencia(resumenes)).Distinct();
        return string.Join("; ", reglas.Select(Describir));
    }

    private static string Describir(Cita cita) => $"{cita.Documento} · {cita.Seccion}";
}
