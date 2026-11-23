using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class OrdenDeReglasPorRelevancia
{
    private readonly IBaseConocimiento _baseConocimiento;

    public OrdenDeReglasPorRelevancia(IBaseConocimiento baseConocimiento)
    {
        _baseConocimiento = baseConocimiento;
    }

    public IReadOnlyList<Cita> Ordenar(IReadOnlyList<Cita> bloque, string pregunta)
    {
        var raicesDeLaPregunta = RaicesDeContenido.De(pregunta).ToList();
        if (bloque.Count == 0 || raicesDeLaPregunta.Count == 0)
        {
            return bloque;
        }

        return [.. bloque.OrderByDescending(cita => Relevancia(_baseConocimiento.ObtenerSeccion(cita), raicesDeLaPregunta))];
    }

    private static int Relevancia(SeccionDocumento? seccion, IReadOnlyList<string> raicesDeLaPregunta)
    {
        if (seccion is null)
        {
            return 0;
        }

        var raicesDeLaSeccion = RaicesDeContenido.De($"{seccion.Titulo} {seccion.Texto}").ToHashSet(StringComparer.Ordinal);
        return raicesDeLaPregunta.Count(raicesDeLaSeccion.Contains);
    }
}
