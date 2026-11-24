using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests;

public static class AfirmacionesDeContexto
{
    private const string PropiedadDeAuditoria = "ContextoTruncado";

    public static bool ContextoTruncadoRegistrado(EventoAuditoria evento) =>
        typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria)?.GetValue(evento) is true;

    public static void AfirmarContextoTruncadoRegistrado(EventoAuditoria evento)
    {
        var propiedad = typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria);
        Assert.True(propiedad is not null, "EventoAuditoria no define ContextoTruncado (plan §4.4 y §12, D-57): se espera bool ContextoTruncado { get; init; }.");
        Assert.True(propiedad.GetValue(evento) is true, "el evento debía llevar contextoTruncado: true");
    }
}
