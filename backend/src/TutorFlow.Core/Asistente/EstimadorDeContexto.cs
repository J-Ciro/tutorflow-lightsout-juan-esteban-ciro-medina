namespace TutorFlow.Core.Asistente;

public static class EstimadorDeContexto
{
    private const decimal CaracteresPorToken = 3.5m;
    private const decimal ProporcionMinimaDelContexto = 0.75m;

    public static int TokensEstimados(SolicitudLlm solicitud)
    {
        var caracteres = solicitud.PromptSistema.Length + solicitud.Mensajes.Sum(mensaje => mensaje.Contenido.Length);
        return (int)Math.Ceiling(caracteres / CaracteresPorToken);
    }

    public static ContextoTruncadoDetectado? Evaluar(SolicitudLlm solicitud, UsoTokens uso)
    {
        var estimados = TokensEstimados(solicitud);
        var minimo = (int)Math.Ceiling(ProporcionMinimaDelContexto * estimados);
        return uso.Entrada < minimo ? new ContextoTruncadoDetectado(estimados, uso.Entrada) : null;
    }
}
