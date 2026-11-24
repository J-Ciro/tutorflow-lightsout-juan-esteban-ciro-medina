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
            RegistrarSinProveedor(proveedor.Conflicto);
            return Task.CompletedTask;
        }

        var temperatura = ServiciosTutorFlow.AjustesDelModelo(_configuracion).Temperatura.ToString(CultureInfo.InvariantCulture);
        RegistrarProveedor(_logger, proveedor.Proveedor, proveedor.Adaptador, proveedor.Modelo, proveedor.PrecioDesconocido, temperatura);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RegistrarSinProveedor(ConflictoDeProveedor? conflicto)
    {
        if (conflicto is null)
        {
            RegistrarProveedorNoDeterminable(_logger);
            return;
        }

        RegistrarConflictoDeProveedor(_logger, conflicto.PorModelo, conflicto.PorLlave);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo determinar el proveedor: define LLM_PROVEEDOR, LLM_MODELO o LLM_URL_BASE")]
    private static partial void RegistrarProveedorNoDeterminable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Conflicto de proveedor: el modelo indica {PorModelo} y la llave indica {PorLlave}; define LLM_PROVEEDOR. Las preguntas que necesitan el LLM responderán 503.")]
    private static partial void RegistrarConflictoDeProveedor(ILogger logger, ProveedorLlm porModelo, ProveedorLlm porLlave);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proveedor del LLM: {Proveedor}, adaptador {Adaptador}, modelo {Modelo}, precio desconocido: {PrecioDesconocido}, temperatura {Temperatura}.")]
    private static partial void RegistrarProveedor(ILogger logger, ProveedorLlm proveedor, AdaptadorLlm adaptador, string modelo, bool precioDesconocido, string temperatura);
}
