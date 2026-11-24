using Microsoft.AspNetCore.Hosting;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api;

internal sealed partial class ValidacionDeArranque : IStartupFilter
{
    private const string ClaveTasaPorIp = "Limites:PreguntasPorMinutoPorIp";
    private const string ClavePresupuestoDiario = "Limites:PresupuestoDiarioUsd";

    private readonly IConfiguration _configuracion;
    private readonly ILogger _logger;

    public ValidacionDeArranque(IConfiguration configuracion, ILoggerFactory fabricaDeLogs)
    {
        _configuracion = configuracion;
        _logger = fabricaDeLogs.CreateLogger("TutorFlow.Configuracion");
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente)
    {
        var efectiva = _configuracion;
        var errores = ValidadorDeConfiguracion.Validar(efectiva);
        if (errores.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errores));
        }

        foreach (var advertencia in ValidadorDeConfiguracion.Advertencias(efectiva))
        {
            RegistrarAdvertencia(_logger, advertencia);
        }

        var tasa = TasaEfectiva(efectiva);
        RegistrarTasaPorIp(_logger, tasa);
        RegistrarPresupuestoSinTope(efectiva);
        return siguiente;
    }

    private static int TasaEfectiva(IConfiguration efectiva) =>
        (int?)ValidadorDeConfiguracion.Efectivo(efectiva, ClaveTasaPorIp) ?? LimitesAsistente.PreguntasPorMinutoPorIpPorDefecto;

    private void RegistrarPresupuestoSinTope(IConfiguration efectiva)
    {
        if (string.IsNullOrWhiteSpace(efectiva[ClavePresupuestoDiario]))
        {
            RegistrarLimiteSinTope(_logger, ClavePresupuestoDiario);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Límite sin tope: {Clave}")]
    private static partial void RegistrarLimiteSinTope(ILogger logger, string clave);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tasa por IP: {Preguntas} preguntas por minuto")]
    private static partial void RegistrarTasaPorIp(ILogger logger, int preguntas);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Mensaje}")]
    internal static partial void RegistrarAdvertencia(ILogger logger, string mensaje);
}
