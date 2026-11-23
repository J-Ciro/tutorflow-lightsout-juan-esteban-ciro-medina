using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed class ClienteLlmNoConfigurado : IClienteLlm
{
    public Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken) =>
        throw new DependenciaNoDisponibleException("No hay un proveedor de LLM configurado.");
}

public static class FabricaClienteLlm
{
    public static IClienteLlm Crear(ProveedorResuelto proveedor, string? llave, HttpClient httpClient) => proveedor.Adaptador switch
    {
        AdaptadorLlm.Anthropic => new ClienteLlmAnthropic(httpClient, proveedor, llave ?? string.Empty),
        AdaptadorLlm.CompatibleOpenAi => new ClienteLlmCompatibleOpenAi(httpClient, proveedor, llave),
        _ => new ClienteLlmNoConfigurado()
    };
}
