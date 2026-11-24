using TutorFlow.Core.Autorizacion;
using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal static class SeccionDeDerogacion
{
    private const string PalabraDelReglamentoDerogado = "2023";

    private static readonly Cita Derogacion = new(ReglasAplicadas.DocumentoReglamento, Textos.SeccionDerogacion);

    public static IReadOnlyList<SeccionDocumento> Agregar(IReadOnlyList<SeccionDocumento> entregadas, string pregunta, IBaseConocimiento baseConocimiento)
    {
        if (!TokenizadorTexto.Valores(pregunta).Contains(PalabraDelReglamentoDerogado) || entregadas.Any(EsLaDerogacion))
        {
            return entregadas;
        }

        var seccion = baseConocimiento.ObtenerSeccion(Derogacion);
        return seccion is null ? entregadas : [.. entregadas, seccion];
    }

    private static bool EsLaDerogacion(SeccionDocumento seccion) =>
        seccion.Documento == Derogacion.Documento && seccion.Seccion == Derogacion.Seccion;
}
