namespace TutorFlow.Core.Asistente;

public sealed record ConfiguracionAsistente(
    PrecioModelo? Precio,
    string? MarcadorCanario,
    IReadOnlyCollection<string> CodigosAsignatura);
