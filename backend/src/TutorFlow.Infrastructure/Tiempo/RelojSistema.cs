using TutorFlow.Core.Asistente;

namespace TutorFlow.Infrastructure.Tiempo;

public sealed class RelojSistema : IReloj
{
    public DateTimeOffset AhoraUtc => DateTimeOffset.UtcNow;
}
