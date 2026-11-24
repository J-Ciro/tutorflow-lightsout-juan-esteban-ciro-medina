using TutorFlow.Core.Academico;

namespace TutorFlow.Core.Asistente;

internal sealed record ActivacionResumen(
    bool Directa,
    IReadOnlyList<string> CodigosNombrados,
    IReadOnlyList<NotasAsignatura>? NotasVerificadas = null,
    bool Matriculada = false)
{
    public static ActivacionResumen Ninguna { get; } = new(false, []);

    public bool Candidata => Directa || CodigosNombrados.Count > 0;

    public bool Entrega => Directa || Matriculada;

    public bool RequiereVerificacion => !Directa && CodigosNombrados.Count > 0;
}
