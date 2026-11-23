namespace TutorFlow.Api;

public static class CorrelacionDePeticion
{
    public const string Encabezado = "X-Correlation-Id";

    public static IApplicationBuilder UsarCorrelacion(this IApplicationBuilder aplicacion) =>
        aplicacion.Use(async (contexto, siguiente) =>
        {
            var correlationId = Guid.CreateVersion7().ToString();
            contexto.TraceIdentifier = correlationId;
            contexto.Response.OnStarting(() =>
            {
                contexto.Response.Headers[Encabezado] = correlationId;
                return Task.CompletedTask;
            });
            await siguiente();
        });
}
