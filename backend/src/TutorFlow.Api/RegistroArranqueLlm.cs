using System.Globalization;
using TutorFlow.Infrastructure.Llm;

namespace TutorFlow.Api;

public sealed partial class RegistroArranqueLlm : IHostedService
{
    private readonly IServiceProvider _servicios;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<RegistroArranqueLlm> _logger;

    public RegistroArranqueLlm(IServiceProvider servicios, IConfiguration configuracion, ILogger<RegistroArranqueLlm> logger)
    {
        _servicios = servicios;
        _configuracion = configuracion;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var proveedor = _servicios.GetRequiredService<ProveedorResuelto>();
        if (proveedor.Adaptador == AdaptadorLlm.NoConfigurado)
        {
            RegistrarConfiguracionIncompleta(_logger);
            return Task.CompletedTask;
        }

        var temperatura = ServiciosTutorFlow.AjustesDelModelo(_configuracion).Temperatura.ToString(CultureInfo.InvariantCulture);
        RegistrarProveedor(_logger, proveedor.Proveedor, proveedor.Adaptador, proveedor.Modelo, proveedor.PrecioDesconocido, temperatura);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuración del LLM incompleta: la llave tiene un prefijo desconocido y falta LLM_URL_BASE. Las preguntas que necesitan el LLM responderán 503.")]
    private static partial void RegistrarConfiguracionIncompleta(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proveedor del LLM: {Proveedor}, adaptador {Adaptador}, modelo {Modelo}, precio desconocido: {PrecioDesconocido}, temperatura {Temperatura}.")]
    private static partial void RegistrarProveedor(ILogger logger, ProveedorLlm proveedor, AdaptadorLlm adaptador, string modelo, bool precioDesconocido, string temperatura);
}
