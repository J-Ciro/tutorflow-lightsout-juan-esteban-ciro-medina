using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class IndiceAprobacionYHabilitacion : IBaseConocimiento
{
    public static readonly SeccionDocumento Articulo5 = new(
        "reglamento_academico_2025.md",
        "Artículo 5",
        "Nota mínima",
        "Una asignatura se aprueba con nota definitiva igual o superior a 3.0 en una escala de 0.0 a 5.0.");

    public static readonly SeccionDocumento Articulo6 = new(
        "reglamento_academico_2025.md",
        "Artículo 6",
        "Habilitación",
        "El estudiante que repruebe una asignatura con nota definitiva igual o superior a 2.0 podrá presentar un examen de habilitación, siempre que no la haya perdido por inasistencia. La nota del examen reemplaza la definitiva y no puede superar 3.0.");

    private static readonly SeccionDocumento[] Todas = [Articulo5, Articulo6];

    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) => Todas;

    public bool ExisteCita(Cita cita) => ObtenerSeccion(cita) is not null;

    public SeccionDocumento? ObtenerSeccion(Cita cita) =>
        Todas.FirstOrDefault(s => s.Documento == cita.Documento && s.Seccion == cita.Seccion);
}
