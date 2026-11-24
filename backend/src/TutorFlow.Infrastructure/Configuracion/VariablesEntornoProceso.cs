namespace TutorFlow.Infrastructure.Configuracion;

public sealed class VariablesEntornoProceso : IVariablesEntorno
{
    public string? Leer(string nombre) => Environment.GetEnvironmentVariable(nombre);
}
