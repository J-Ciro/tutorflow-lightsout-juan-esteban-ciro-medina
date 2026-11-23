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

    public DependenciaNoDisponibleException(string message, string? retryAfterDelProveedor)
        : base(message)
    {
        RetryAfterDelProveedor = retryAfterDelProveedor;
    }

    public string? RetryAfterDelProveedor { get; }
}
