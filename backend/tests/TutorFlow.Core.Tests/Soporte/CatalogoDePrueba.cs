using TutorFlow.Core.Asistente;

namespace TutorFlow.Pruebas.Soporte;

public static class CatalogoDePrueba
{
    public static IReadOnlyList<AsignaturaCatalogo> Asignaturas { get; } =
    [
        new("CAL-201", "Cálculo Integral"),
        new("POO-202", "Programación Orientada a Objetos"),
        new("BD-203", "Bases de Datos"),
        new("ING-204", "Inglés III"),
        new("CON-101", "Contabilidad"),
        new("MIC-102", "Microeconomía"),
        new("EST-103", "Estadística"),
        new("COM-104", "Comunicación Oral y Escrita")
    ];
}
