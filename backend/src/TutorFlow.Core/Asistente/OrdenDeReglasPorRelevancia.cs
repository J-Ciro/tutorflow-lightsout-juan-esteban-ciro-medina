using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class OrdenDeReglasPorRelevancia
{
    private const int MaximoSeccionesConsultadas = int.MaxValue;

    private const string ConsultaQueAlcanzaLasReglas =
        "nota definitiva promedio ponderado asistencia inasistencia faltas perdida perdidas habilitacion prueba academica beca rendimiento sin registro";

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

        var secciones = _baseConocimiento.BuscarSecciones(ConsultaQueAlcanzaLasReglas, MaximoSeccionesConsultadas);
        return [.. bloque.OrderByDescending(cita => Relevancia(cita, secciones, raicesDeLaPregunta))];
    }

    private static int Relevancia(Cita cita, IReadOnlyList<SeccionDocumento> secciones, IReadOnlyList<string> raicesDeLaPregunta)
    {
        var seccion = secciones.FirstOrDefault(s => s.Documento == cita.Documento && s.Seccion == cita.Seccion);
        if (seccion is null)
        {
            return 0;
        }

        var raicesDeLaSeccion = RaicesDeContenido.De($"{seccion.Titulo} {seccion.Texto}").ToHashSet(StringComparer.Ordinal);
        return raicesDeLaPregunta.Count(raicesDeLaSeccion.Contains);
    }
}
