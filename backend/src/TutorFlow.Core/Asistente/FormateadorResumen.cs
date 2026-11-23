using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed record ResumenEntregado(string Texto, IReadOnlyList<int> EntradasDeReglas);

internal static class FormateadorResumen
{
    public static ResumenEntregado Resumir(ResumenEstudiante resumen)
    {
        var entradas = ReglasAplicadas.DeNotasYAsistencia([resumen]);
        var lineas = new List<string>(FormateadorDatos.LineasDeNotas(resumen)) { FormateadorDatos.Asistencia(resumen) };
        lineas.Add(LineaDeReglas(ReglasAplicadas.Citas(entradas)));
        return new ResumenEntregado(string.Join("\n", lineas), entradas);
    }

    public static string LineaDeReglas(IReadOnlyList<Cita> reglas) =>
        $"Reglas aplicadas a estos datos: {string.Join("; ", reglas.Select(Describir))}.";

    private static string Describir(Cita cita) => $"{cita.Documento} · {cita.Seccion}";
}
