using TutorFlow.Core.Asistente;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class IndiceBiblioteca : IBaseConocimiento
{
    public const string Cita = "documento_prueba.md · Artículo 1";

    public static readonly SeccionDocumento Seccion = new(
        "documento_prueba.md",
        "Artículo 1",
        "Biblioteca",
        "La biblioteca atiende de lunes a viernes en el edificio C.");

    public IReadOnlyList<SeccionDocumento> BuscarSecciones(string consulta, int maximo) => [Seccion];


    public SeccionDocumento? ObtenerSeccion(Cita cita) =>
        cita.Documento == Seccion.Documento && cita.Seccion == Seccion.Seccion ? Seccion : null;
}
