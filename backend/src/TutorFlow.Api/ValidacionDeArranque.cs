using TutorFlow.Infrastructure.Configuracion;

namespace TutorFlow.Api;

internal sealed partial class ValidacionDeArranque : IHostedLifecycleService
{
    private static readonly string[] VariablesDePrecio = ["LLM_PRECIO_ENTRADA", "LLM_PRECIO_SALIDA"];

    private readonly IConfiguration _configuracion;
    private readonly IVariablesEntorno _variables;
    private readonly ILogger _logger;

    public ValidacionDeArranque(IConfiguration configuracion, IVariablesEntorno variables, ILoggerFactory fabricaDeLogs)
    {
        _configuracion = configuracion;
        _variables = variables;
        _logger = fabricaDeLogs.CreateLogger("TutorFlow.Configuracion");
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        var efectiva = ConEntorno();
        var errores = ValidadorDeConfiguracion.Validar(efectiva);
        if (errores.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errores));
        }

        foreach (var advertencia in ValidadorDeConfiguracion.Advertencias(efectiva))
        {
            RegistrarAdvertencia(_logger, advertencia);
        }

        AnularValoresIgnorados(ValidadorDeConfiguracion.ClavesParaAnular(efectiva));
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IConfiguration ConEntorno()
    {
        var delEntorno = VariablesDePrecio
            .Select(nombre => new KeyValuePair<string, string?>(nombre, _variables.Leer(nombre)))
            .Where(par => par.Value is not null);
        return new ConfigurationBuilder().AddConfiguration(_configuracion).AddInMemoryCollection(delEntorno).Build();
    }

    private void AnularValoresIgnorados(IEnumerable<string> claves)
    {
        if (_configuracion is IConfigurationBuilder constructor)
        {
            constructor.AddInMemoryCollection(claves.Select(clave => new KeyValuePair<string, string?>(clave, null)));
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Mensaje}")]
    internal static partial void RegistrarAdvertencia(ILogger logger, string mensaje);
}
