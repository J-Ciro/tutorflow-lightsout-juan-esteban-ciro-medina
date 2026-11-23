using TutorFlow.Core.Conocimiento;

namespace TutorFlow.Core.Asistente;

internal sealed class CitasDeRespuesta
{
    private const int MaximoCitasDeRespaldo = 2;

    private readonly int _apoyoMinimo;

    public CitasDeRespuesta(int apoyoMinimo)
    {
        _apoyoMinimo = apoyoMinimo;
    }

    public IReadOnlyList<Cita> EscritasConApoyo(string texto, IReadOnlyList<SeccionDocumento> entregadas, IReadOnlyCollection<Cita> reglasDeDatos)
    {
        var apoyadas = SeccionesConApoyo(texto, entregadas).Select(CitaDe).ToHashSet();
        return [.. ExtractorCitas.De(texto).Where(cita => apoyadas.Contains(cita) || reglasDeDatos.Contains(cita))];
    }

    public IReadOnlyList<Cita> Respaldo(string texto, IReadOnlyList<SeccionDocumento> entregadas) =>
        [.. SeccionesConApoyo(texto, entregadas).Take(MaximoCitasDeRespaldo).Select(CitaDe)];

    private IEnumerable<SeccionDocumento> SeccionesConApoyo(string texto, IReadOnlyList<SeccionDocumento> entregadas)
    {
        var raicesDeLaRespuesta = RaicesDeContenido.DeApoyo(ExtractorCitas.SinCitas(texto)).ToHashSet(StringComparer.Ordinal);
        return entregadas.Where(seccion => RaicesDeContenido.DeApoyo($"{seccion.Titulo} {seccion.Texto}").Count(raicesDeLaRespuesta.Contains) >= _apoyoMinimo);
    }

    private static Cita CitaDe(SeccionDocumento seccion) => new(seccion.Documento, seccion.Seccion);
}
