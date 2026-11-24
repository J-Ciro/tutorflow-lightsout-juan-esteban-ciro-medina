using TutorFlow.Core.Academico;
using TutorFlow.Core.Autorizacion;

namespace TutorFlow.Core.Asistente;

internal sealed record ContextoDeAfirmaciones(
    Usuario Usuario,
    IReadOnlyCollection<string> IdsAutorizados,
    DirectorioAcademico Directorio,
    IReadOnlyDictionary<string, ResumenEstudiante> Resumenes)
{
    public PersonasDeBloque PersonasDe(BloqueDeTexto bloque) =>
        TramosDePersonas.De(bloque, Usuario, IdsAutorizados, Directorio);

    public bool Cumple(string personaId, Func<ResumenEstudiante, bool> exigencia) =>
        Resumenes.TryGetValue(personaId, out var resumen) && exigencia(resumen);
}
