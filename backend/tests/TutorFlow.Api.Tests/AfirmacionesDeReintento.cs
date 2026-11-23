using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests;

public static class AfirmacionesDeReintento
{
    private const string PropiedadDeAuditoria = "Reintento";

    public static bool ReintentoRegistrado(EventoAuditoria evento) =>
        typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria)?.GetValue(evento) is true;

    public static void AfirmarReintentoRegistrado(EventoAuditoria evento)
    {
        var propiedad = typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria);
        Assert.True(propiedad is not null, "EventoAuditoria no define Reintento (plan §11.5 y §12, D-48): se espera bool Reintento { get; init; }.");
        Assert.True(propiedad.GetValue(evento) is true, "el evento debía llevar reintento: true");
    }
}
