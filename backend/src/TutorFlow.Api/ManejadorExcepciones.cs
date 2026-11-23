using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TutorFlow.Api;

public sealed partial class ManejadorExcepciones : IExceptionHandler
{
    private const string TituloErrorInesperado = "Ocurrió un error inesperado.";
    private const string TipoDeContenidoProblema = "application/problem+json";

    private readonly ILogger<ManejadorExcepciones> _logger;

    public ManejadorExcepciones(ILogger<ManejadorExcepciones> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        RegistrarError(_logger, exception);
        var problema = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = TituloErrorInesperado
        };
        problema.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problema, options: null, contentType: TipoDeContenidoProblema, cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado")]
    private static partial void RegistrarError(ILogger logger, Exception exception);
}
