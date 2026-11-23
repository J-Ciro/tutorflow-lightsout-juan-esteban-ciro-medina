using TutorFlow.Core.Academico;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

public static class ReglasAplicadas
{
    public const string DocumentoReglamento = "reglamento_academico_2025.md";
    public const string DocumentoEvaluacion = "evaluacion_y_calificaciones.md";
    public const string DocumentoBienestar = "bienestar_y_becas.md";

    private static readonly IReadOnlyList<ReglaCondicionada> OrdenFijo =
    [
        new(new Cita(DocumentoEvaluacion, Textos.SeccionDefinitiva), lectura => lectura.Notas),
        new(new Cita(DocumentoEvaluacion, Textos.SeccionPromedio), lectura => lectura.Notas),
        new(new Cita(DocumentoReglamento, Textos.SeccionPerdidaPorInasistencia), lectura => lectura.Asistencia),
        new(new Cita(DocumentoBienestar, Textos.SeccionBecaPorRendimiento), lectura => lectura.Notas),
        new(new Cita(DocumentoReglamento, Textos.SeccionNotaMinima), lectura => lectura.Hay(EstadoAsignatura.Perdida)),
        new(new Cita(DocumentoReglamento, Textos.SeccionHabilitacion), lectura => lectura.Hay(EstadoAsignatura.Perdida)),
        new(new Cita(DocumentoReglamento, Textos.SeccionNoHabilitables), lectura => lectura.Hay(EstadoAsignatura.PerdidaPorInasistencia)),
        new(new Cita(DocumentoReglamento, Textos.SeccionPerdidaPorInasistencia), lectura => lectura.Hay(EstadoAsignatura.PerdidaPorInasistencia)),
        new(new Cita(DocumentoReglamento, Textos.SeccionPruebaAcademica), lectura => lectura.PruebaAcademica),
        new(new Cita(DocumentoReglamento, Textos.SeccionAsistenciaSinRegistro), lectura => lectura.AsistenciaSinRegistro),
        new(new Cita(DocumentoEvaluacion, Textos.SeccionDefinitiva), lectura => lectura.Hay(EstadoAsignatura.SinDefinitiva))
    ];

    public static IReadOnlyList<int> DeNotas(IReadOnlyList<ResumenEstudiante> resumenes) =>
        Entradas(new LecturaDeReglas(true, false, resumenes));

    public static IReadOnlyList<int> DeAsistencia(IReadOnlyList<ResumenEstudiante> resumenes) =>
        Entradas(new LecturaDeReglas(false, true, resumenes));

    public static IReadOnlyList<int> DeNotasYAsistencia(IReadOnlyList<ResumenEstudiante> resumenes) =>
        Entradas(new LecturaDeReglas(true, true, resumenes));

    public static IReadOnlyList<int> DePrograma(IReadOnlyList<ResumenEstudiante> resumenes) =>
        Entradas(new LecturaDeReglas(false, false, resumenes));

    public static IReadOnlyList<Cita> Citas(IEnumerable<int> entradas) =>
        [.. entradas.Distinct().Order().Select(entrada => OrdenFijo[entrada].Cita).Distinct()];

    private static List<int> Entradas(LecturaDeReglas lectura) =>
        [.. Enumerable.Range(0, OrdenFijo.Count).Where(entrada => OrdenFijo[entrada].Aplica(lectura))];

    private sealed record ReglaCondicionada(Cita Cita, Func<LecturaDeReglas, bool> Aplica);

    private sealed record LecturaDeReglas(bool Notas, bool Asistencia, IReadOnlyList<ResumenEstudiante> Resumenes)
    {
        private IEnumerable<ResultadoAsignatura> Resultados => Resumenes.SelectMany(resumen => resumen.Asignaturas);

        public bool PruebaAcademica => Resumenes.Any(resumen => resumen.PruebaAcademica);

        public bool AsistenciaSinRegistro => Resultados.Any(r => r.EstadoAsistencia == EstadoAsistencia.SinRegistro);

        public bool Hay(EstadoAsignatura estado) => Resultados.Any(r => r.Estado == estado);
    }
}
