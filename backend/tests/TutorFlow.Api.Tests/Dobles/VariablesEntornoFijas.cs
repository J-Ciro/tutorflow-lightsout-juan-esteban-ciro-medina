using TutorFlow.Infrastructure.Configuracion;

namespace TutorFlow.Api.Tests.Dobles;

public sealed class VariablesEntornoFijas(IReadOnlyDictionary<string, string?> valores) : IVariablesEntorno
{
    public string? Leer(string nombre) => valores.GetValueOrDefault(nombre);
}
