using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Tests.Asistente;

internal static class SeccionesDeReferencia
{
    public const string Reglamento = "reglamento_academico_2025.md";
    public const string Evaluacion = "evaluacion_y_calificaciones.md";
    public const string Bienestar = "bienestar_y_becas.md";
    public const string DocumentoDePrueba = "documento_prueba.md";

    public static readonly SeccionDocumento Articulo5 = new(Reglamento, "Artículo 5", "Nota mínima", "Una asignatura se aprueba con nota definitiva igual o superior a 3.0 en una escala de 0.0 a 5.0.");
    public static readonly SeccionDocumento Articulo11 = new(Reglamento, "Artículo 11", "Pérdida por inasistencia", "El estudiante que falte a más del 15% de las clases programadas de una asignatura la pierde por inasistencia, con nota definitiva de 0.0, sin importar las notas obtenidas en los cortes.");
    public static readonly SeccionDocumento Articulo12 = new(Reglamento, "Artículo 12", "Excusas", "Las inasistencias justificadas con excusa médica o institucional aprobada no se cuentan para el límite del artículo 11. Las excusas se registran en el sistema académico; si una inasistencia no aparece registrada, el estudiante debe solicitar la revisión a su coordinador.");
    public static readonly SeccionDocumento Articulo22 = new(Reglamento, "Artículo 22", "Protección de datos", "La información académica de cada estudiante es personal. Solo pueden consultarla el propio estudiante, el coordinador de su programa y las dependencias autorizadas, conforme a la Ley 1581 de 2012.");
    public static readonly SeccionDocumento Atencion = new(Bienestar, "Sección 1", "Atención", "La oficina de Bienestar atiende en el edificio B, piso 2: Lunes a viernes: 8:00 a 17:00. Sábados: 8:00 a 12:00. Correo: bienestar@horizonte.edu.co (ficticio).");
    public static readonly SeccionDocumento Servicios = new(Bienestar, "Sección 2", "Servicios", "Asesoría psicológica: con cita previa, sin costo. Tutorías académicas: martes y jueves de 14:00 a 18:00 en la biblioteca. Deportes y cultura: inscripciones en las dos primeras semanas de cada periodo.");
    public static readonly SeccionDocumento BecaPorRendimiento = new(Bienestar, "Sección 3.1", "Beca por rendimiento académico", "Descuento del 50% en la matrícula del periodo siguiente. Requisitos: promedio ponderado del periodo igual o superior a 4.3; ninguna asignatura perdida en el periodo, incluidas las perdidas por inasistencia; haber matriculado al menos 12 créditos.");
    public static readonly SeccionDocumento BecaSocioeconomica = new(Bienestar, "Sección 3.2", "Beca de apoyo socioeconómico", "Descuento de hasta el 30% para estudiantes de estratos 1 y 2. Se solicita en Bienestar con los documentos que acrediten la situación del hogar. Los cupos son limitados y se asignan por orden de llegada.");
    public static readonly SeccionDocumento Biblioteca = new(DocumentoDePrueba, "Artículo 1", "Biblioteca", "La biblioteca atiende de lunes a viernes en el edificio C.");

    public static IndiceFijo Indice(params SeccionDocumento[] entregadas) => new(entregadas, entregadas);

    public static IndiceFijo Indice(IReadOnlyList<SeccionDocumento> indexadas, IReadOnlyList<SeccionDocumento> entregadas) => new(indexadas, entregadas);
}
