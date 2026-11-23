using TutorFlow.Infrastructure.Llm;

namespace TutorFlow.Api;

public sealed partial class RegistroArranqueLlm : IHostedService
{
    private readonly ProveedorResuelto _proveedor;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<RegistroArranqueLlm> _logger;

    public RegistroArranqueLlm(ProveedorResuelto proveedor, IConfiguration configuracion, ILogger<RegistroArranqueLlm> logger)
    {
        _proveedor = proveedor;
        _configuracion = configuracion;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AdvertirSiLaReservaSinPrecioEsInvalida();
        if (_proveedor.Adaptador == AdaptadorLlm.NoConfigurado)
        {
            RegistrarConfiguracionIncompleta(_logger);
            return Task.CompletedTask;
        }

        RegistrarProveedor(_logger, _proveedor.Proveedor, _proveedor.Adaptador, _proveedor.Modelo, _proveedor.PrecioDesconocido);
        return Task.CompletedTask;
    }

    private void AdvertirSiLaReservaSinPrecioEsInvalida()
    {
        var configurada = _configuracion[ServiciosTutorFlow.ClaveReservaSinPrecio];
        if (configurada is not null && ServiciosTutorFlow.LeerReservaSinPrecio(_configuracion) is null)
        {
            RegistrarReservaSinPrecioInvalida(_logger, configurada);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Llm:ReservaSinPrecioUsd debe ser un decimal mayor que cero; se ignora el valor \"{Valor}\" y se usa la reserva por defecto.")]
    private static partial void RegistrarReservaSinPrecioInvalida(ILogger logger, string valor);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuración del LLM incompleta: la llave tiene un prefijo desconocido y falta LLM_URL_BASE. Las preguntas que necesitan el LLM responderán 503.")]
    private static partial void RegistrarConfiguracionIncompleta(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Proveedor del LLM: {Proveedor}, adaptador {Adaptador}, modelo {Modelo}, precio desconocido: {PrecioDesconocido}.")]
    private static partial void RegistrarProveedor(ILogger logger, ProveedorLlm proveedor, AdaptadorLlm adaptador, string modelo, bool precioDesconocido);
}
