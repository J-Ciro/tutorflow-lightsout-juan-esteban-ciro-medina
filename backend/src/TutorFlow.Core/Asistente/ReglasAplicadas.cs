using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static class ReglasAplicadas
{
    public const string DocumentoReglamento = "reglamento_academico_2025.md";
    public const string DocumentoEvaluacion = "evaluacion_y_calificaciones.md";
    public const string DocumentoBienestar = "bienestar_y_becas.md";

    private static readonly Cita DefinitivaEvaluacion = new(DocumentoEvaluacion, "Sección 3");
    private static readonly Cita PromedioEvaluacion = new(DocumentoEvaluacion, "Sección 4");
    private static readonly Cita NotaMinima = new(DocumentoReglamento, "Artículo 5");
    private static readonly Cita Habilitacion = new(DocumentoReglamento, "Artículo 6");
    private static readonly Cita NoHabilitables = new(DocumentoReglamento, "Artículo 7");
    private static readonly Cita PerdidaPorInasistencia = new(DocumentoReglamento, "Artículo 11");
    private static readonly Cita AsistenciaSinRegistro = new(DocumentoReglamento, "Artículo 12");
    private static readonly Cita PruebaAcademica = new(DocumentoReglamento, "Artículo 20");
    private static readonly Cita BecaPorRendimiento = new(DocumentoBienestar, "Sección 3.1");

    public static IReadOnlyList<Cita> DeNotas(IReadOnlyList<ResumenEstudiante> resumenes) =>
        [.. new[] { DefinitivaEvaluacion, PromedioEvaluacion, BecaPorRendimiento }.Concat(Condicionales(resumenes)).Distinct()];

    public static IReadOnlyList<Cita> DeAsistencia(IReadOnlyList<ResumenEstudiante> resumenes) =>
        [.. new[] { PerdidaPorInasistencia }.Concat(Condicionales(resumenes)).Distinct()];

    public static IReadOnlyList<Cita> DePrograma(IReadOnlyList<ResumenEstudiante> resumenes) =>
        [.. Condicionales(resumenes).Distinct()];

    private static IEnumerable<Cita> Condicionales(IReadOnlyList<ResumenEstudiante> resumenes)
    {
        var resultados = resumenes.SelectMany(resumen => resumen.Asignaturas).ToList();
        if (resultados.Any(r => r.Estado == EstadoAsignatura.Perdida))
        {
            yield return NotaMinima;
            yield return Habilitacion;
        }

        if (resultados.Any(r => r.Estado == EstadoAsignatura.PerdidaPorInasistencia))
        {
            yield return NoHabilitables;
            yield return PerdidaPorInasistencia;
        }

        foreach (var cita in CondicionalesDeEstado(resumenes, resultados))
        {
            yield return cita;
        }
    }

    private static IEnumerable<Cita> CondicionalesDeEstado(IReadOnlyList<ResumenEstudiante> resumenes, List<ResultadoAsignatura> resultados)
    {
        if (resumenes.Any(resumen => resumen.PruebaAcademica))
        {
            yield return PruebaAcademica;
        }

        if (resultados.Any(r => r.EstadoAsistencia == EstadoAsistencia.SinRegistro))
        {
            yield return AsistenciaSinRegistro;
        }

        if (resultados.Any(r => r.Estado == EstadoAsignatura.SinDefinitiva))
        {
            yield return DefinitivaEvaluacion;
        }
    }
}
