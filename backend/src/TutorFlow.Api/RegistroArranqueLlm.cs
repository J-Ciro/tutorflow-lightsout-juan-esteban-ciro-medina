using TutorFlow.Infrastructure.Llm;

namespace TutorFlow.Api;

public sealed partial class RegistroArranqueLlm : IHostedService
{
    private readonly IServiceProvider _servicios;
    private readonly ILogger<RegistroArranqueLlm> _logger;

    public RegistroArranqueLlm(IServiceProvider servicios, ILogger<RegistroArranqueLlm> logger)
    {
        _servicios = servicios;
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

        RegistrarProveedor(_logger, proveedor.Proveedor, proveedor.Adaptador, proveedor.Modelo, proveedor.PrecioDesconocido);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuración del LLM incompleta: la llave tiene un prefijo desconocido y falta LLM_URL_BASE. Las preguntas que necesitan el LLM responderán 503.")]
    private static partial void RegistrarConfiguracionIncompleta(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proveedor del LLM: {Proveedor}, adaptador {Adaptador}, modelo {Modelo}, precio desconocido: {PrecioDesconocido}.")]
    private static partial void RegistrarProveedor(ILogger logger, ProveedorLlm proveedor, AdaptadorLlm adaptador, string modelo, bool precioDesconocido);
}
