using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal static class DatosObligatorios
{
    private static readonly Dictionary<Cita, (string Dato, string Linea)> PorSeccion = new()
    {
        [new Cita(ReglasAplicadas.DocumentoBienestar, "Sección 1")] = ("edificio B, piso 2", "Lugar: edificio B, piso 2.")
    };

    public static string Anexar(string respuesta, IReadOnlyList<Cita> citas)
    {
        var faltantes = PorSeccion
            .Where(fila => citas.Contains(fila.Key) && !Contiene(respuesta, fila.Value.Dato))
            .Select(fila => fila.Value.Linea);
        return string.Join("\n", faltantes.Prepend(respuesta));
    }

    private static bool Contiene(string respuesta, string dato) =>
        NormalizadorTexto.Normalizar(respuesta).Contains(NormalizadorTexto.Normalizar(dato), StringComparison.Ordinal);
}
