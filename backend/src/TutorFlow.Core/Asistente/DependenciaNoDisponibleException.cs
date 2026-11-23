namespace TutorFlow.Core.Asistente;

public sealed class DependenciaNoDisponibleException : Exception
{
    public DependenciaNoDisponibleException()
    {
    }

    public DependenciaNoDisponibleException(string message)
        : base(message)
    {
    }

    public DependenciaNoDisponibleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
