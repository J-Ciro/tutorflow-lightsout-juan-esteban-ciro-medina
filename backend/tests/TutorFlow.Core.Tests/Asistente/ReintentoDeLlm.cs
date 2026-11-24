using TutorFlow.Core.Asistente;
using static TutorFlow.Core.Tests.Asistente.EscenarioAsistente;

namespace TutorFlow.Core.Tests.Asistente;

internal sealed record MensajeCapturado(RolMensaje Rol, string Contenido);

internal sealed class GuionDeDosTurnos(string primerTurno, string reintento, string? herramientaInicial = null)
{
    public List<IReadOnlyList<MensajeCapturado>> Capturas { get; } = [];

    public RespuestaLlm Responder(SolicitudLlm solicitud, int llamada)
    {
        Capturas.Add([.. solicitud.Mensajes.Select(m => new MensajeCapturado(m.Rol, m.Contenido))]);
        if (ReintentoDeLlm.EsReintento(solicitud))
        {
            return Texto(reintento);
        }

        return herramientaInicial is not null && !solicitud.Mensajes.Any(m => m.Rol == RolMensaje.Herramienta)
            ? Herramienta(herramientaInicial)
            : Texto(primerTurno);
    }
}

internal static class ReintentoDeLlm
{
    public const string Prefijo = "Tu respuesta anterior no se pudo verificar: ";
    private const string PropiedadDeAuditoria = "Reintento";

    public static string Instruccion(string motivo) =>
        $"{Prefijo}{motivo}. Responde de nuevo usando solo los datos y las secciones entregados.";

    public static bool EsReintento(SolicitudLlm solicitud) =>
        solicitud.Mensajes.Any(m => m.Rol == RolMensaje.Usuario && m.Contenido.StartsWith(Prefijo, StringComparison.Ordinal));

    public static IReadOnlyList<SolicitudLlm> Reintentos(LlmDoble llm) => [.. llm.Solicitudes.Where(EsReintento)];

    public static bool ReintentoRegistrado(EventoAuditoria evento) =>
        typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria)?.GetValue(evento) is true;

    public static void AfirmarReintentoRegistrado(EventoAuditoria evento)
    {
        var propiedad = typeof(EventoAuditoria).GetProperty(PropiedadDeAuditoria);
        Assert.True(propiedad is not null, "EventoAuditoria no define Reintento (plan §11.5 y §12, D-48): se espera bool Reintento { get; init; }.");
        Assert.True(propiedad.GetValue(evento) is true, "el evento debía llevar reintento: true");
    }

    public static void AfirmarSegundoTurno(GuionDeDosTurnos guion, string primeraRespuesta, string instruccion)
    {
        Assert.True(guion.Capturas.Count >= 2, "el modelo debía recibir al menos 2 llamadas");
        var reintento = guion.Capturas[^1];
        var previa = guion.Capturas[^2];
        Assert.Equal(previa.Count + 2, reintento.Count);
        Assert.Equal(previa, reintento.Take(previa.Count));
        Assert.Equal(new MensajeCapturado(RolMensaje.Asistente, primeraRespuesta), reintento[^2]);
        Assert.Equal(new MensajeCapturado(RolMensaje.Usuario, instruccion), reintento[^1]);
        Assert.Single(guion.Capturas, c => c.Any(m => m.Contenido.StartsWith(Prefijo, StringComparison.Ordinal)));
    }
}
