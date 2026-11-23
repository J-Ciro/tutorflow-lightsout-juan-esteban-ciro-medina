using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed record ResumenEntregado(string Texto, IReadOnlyList<int> EntradasDeReglas);

internal static class FormateadorResumen
{
    public static ResumenEntregado Resumir(ResumenEstudiante resumen)
    {
        var entradas = ReglasAplicadas.DeNotasYAsistencia([resumen]);
        var lineas = resumen.Asignaturas.Select(LineaDeAsignatura).ToList();
        lineas.Add($"Promedio ponderado del periodo: {FormateadorDatos.Promedio(resumen.Promedio)}.");
        lineas.Add($"Créditos matriculados: {resumen.CreditosMatriculados}.");
        lineas.Add($"Prueba académica: {(resumen.PruebaAcademica ? "sí" : "no")}.");
        lineas.Add($"Beca por rendimiento: {FormateadorDatos.TextoBeca(resumen.Beca)}.");
        lineas.Add(LineaDeReglas(ReglasAplicadas.Citas(entradas)));
        return new ResumenEntregado(string.Join("\n", lineas), entradas);
    }

    public static string LineaDeReglas(IReadOnlyList<Cita> reglas) =>
        $"Reglas aplicadas a estos datos: {string.Join("; ", reglas.Select(Describir))}.";

    private static string LineaDeAsignatura(ResultadoAsignatura resultado)
    {
        var asignatura = resultado.Asignatura;
        return $"{asignatura.Nombre} ({asignatura.Codigo}, {asignatura.Creditos} créditos): "
            + $"{FormateadorDatos.DescribirEstado(resultado)}; asistencia: {FormateadorDatos.DescribirAsistencia(resultado)}.";
    }

    private static string Describir(Cita cita) => $"{cita.Documento} · {cita.Seccion}";
}
