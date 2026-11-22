using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Llm;

public sealed class ClienteLlmAnthropic : IClienteLlm
{
    public Task<RespuestaLlm> CompletarAsync(SolicitudLlm solicitud, CancellationToken cancellationToken) => throw new NotImplementedException();
}
