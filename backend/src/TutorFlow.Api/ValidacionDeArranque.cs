using Microsoft.AspNetCore.Hosting;
using TutorFlow.Infrastructure.Configuracion;

namespace TutorFlow.Api;

internal sealed partial class ValidacionDeArranque : IStartupFilter
{
    private static readonly string[] VariablesDePrecio = ["LLM_PRECIO_ENTRADA", "LLM_PRECIO_SALIDA"];
    private static readonly string[] ClavesDeLimitesOpcionales = ["Limites:PreguntasPorMinutoPorIp", "Limites:PresupuestoDiarioUsd"];

    private readonly IConfiguration _configuracion;
    private readonly IVariablesEntorno _variables;
    private readonly ILogger _logger;

    public ValidacionDeArranque(IConfiguration configuracion, IVariablesEntorno variables, ILoggerFactory fabricaDeLogs)
    {
        _configuracion = configuracion;
        _variables = variables;
        _logger = fabricaDeLogs.CreateLogger("TutorFlow.Configuracion");
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente)
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

        RegistrarLimitesSinTope(efectiva);
        RegistrarProxies(efectiva);
        return siguiente;
    }

    private void RegistrarLimitesSinTope(IConfiguration efectiva)
    {
        foreach (var clave in ClavesDeLimitesOpcionales.Where(clave => string.IsNullOrWhiteSpace(efectiva[clave])))
        {
            RegistrarLimiteSinTope(_logger, clave);
        }
    }

    private void RegistrarProxies(IConfiguration efectiva)
    {
        var ips = ProxyConfiable.Efectivas(efectiva);
        if (ips.Count > 0)
        {
            var texto = string.Join(", ", ips);
            RegistrarProxyConfiable(_logger, texto);
        }
    }

    private IConfiguration ConEntorno()
    {
        var delEntorno = VariablesDePrecio
            .Select(nombre => new KeyValuePair<string, string?>(nombre, _variables.Leer(nombre)))
            .Where(par => par.Value is not null);
        return new ConfigurationBuilder().AddConfiguration(_configuracion).AddInMemoryCollection(delEntorno).Build();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Límite sin tope: {Clave}")]
    private static partial void RegistrarLimiteSinTope(ILogger logger, string clave);

    [LoggerMessage(Level = LogLevel.Information, Message = "IPs de proxy de confianza: {Ips}.")]
    private static partial void RegistrarProxyConfiable(ILogger logger, string ips);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Mensaje}")]
    internal static partial void RegistrarAdvertencia(ILogger logger, string mensaje);
}
