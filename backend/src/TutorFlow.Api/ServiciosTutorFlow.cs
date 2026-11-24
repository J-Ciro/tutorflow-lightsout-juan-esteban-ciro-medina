using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Infrastructure.ApiAcademica;
using TutorFlow.Infrastructure.Auditoria;
using TutorFlow.Infrastructure.Configuracion;
using TutorFlow.Infrastructure.Conocimiento;
using TutorFlow.Infrastructure.Llm;
using TutorFlow.Infrastructure.Tiempo;

namespace TutorFlow.Api;

public static class ServiciosTutorFlow
{
    private const string NombreClienteLlm = "llm";
    private const string SeccionAsignaturas = "Autorizacion:Asignaturas";
    private const string ClaveMarcadorCanario = "Llm:MarcadorCanario";
    private const string ClaveReservaPorPeticion = "Llm:ReservaPorPeticionUsd";
    private const string ClaveReservaSinPrecio = "Llm:ReservaSinPrecioUsd";
    private const string ClaveRelevanciaMinima = "Conocimiento:RelevanciaMinima";
    private const string ClaveApoyoMinimoRespaldo = "Conocimiento:ApoyoMinimoRespaldo";
    private const string ClavePuntajeRelativoMinimo = "Conocimiento:PuntajeRelativoMinimo";
    private const string ClavePuntajeRelativoRelevante = "Conocimiento:PuntajeRelativoRelevante";
    private const int PuntajeRelativoMinimoPorDefecto = 55;
    private const int PuntajeRelativoRelevantePorDefecto = 40;
    private const string ClaveTemperatura = "Llm:Temperatura";
    private const string ClaveSemilla = "Llm:Semilla";
    private const int SemillaPorDefecto = 1;
    internal const string ClaveSeccionLimites = "Limites";
    private const string ClaveTraceId = "traceId";
    private const string VariableLlaveLlm = "LLM_API_KEY";

    public static IServiceCollection AgregarTutorFlow(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddTransient<IStartupFilter, ValidacionDeArranque>();
        AgregarOpciones(servicios);
        AgregarNucleo(servicios, configuracion);
        AgregarInfraestructura(servicios, configuracion);
        AgregarLlm(servicios, configuracion);
        servicios.AddProblemDetails(opciones =>
            opciones.CustomizeProblemDetails = contexto => contexto.ProblemDetails.Extensions[ClaveTraceId] = contexto.HttpContext.TraceIdentifier);
        servicios.AddExceptionHandler<ManejadorExcepciones>();
        servicios.ConfigureHttpJsonOptions(opciones => opciones.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        servicios.AddHostedService<RegistroArranqueLlm>();
        return servicios;
    }

    private static void AgregarOpciones(IServiceCollection servicios)
    {
        servicios.AddOptions<OpcionesApiAcademica>()
            .BindConfiguration(OpcionesApiAcademica.Seccion)
            .Validate(o => Uri.IsWellFormedUriString(o.UrlBase, UriKind.Absolute), "ApiAcademica:UrlBase debe ser una URL absoluta")
            .ValidateOnStart();
        servicios.AddOptions<OpcionesLlm>()
            .BindConfiguration(OpcionesLlm.Seccion)
            .Validate(o => o.Proveedores.Count > 0, "Llm:Proveedores no puede estar vacio");
        servicios.AddOptions<LimitesAsistente>()
            .Configure<IConfiguration>(EnlazadorDeLimites.Enlazar)
            .Validate(l => l.MaximoCaracteresPregunta > 0 && l.PreguntasPorMinutoPorIp > 0, "Limites invalidos");
        servicios.AddOptions<OpcionesConocimiento>()
            .BindConfiguration(OpcionesConocimiento.Seccion)
            .Validate(o => o.DocumentosVigentes.Count > 0, "Conocimiento:DocumentosVigentes no puede estar vacio")
            .ValidateOnStart();
    }

    private static void AgregarNucleo(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton(proveedor => proveedor.GetRequiredService<IOptions<LimitesAsistente>>().Value);
        servicios.AddSingleton<PoliticaAcceso>();
        servicios.AddSingleton<VerificadorSalida>();
        servicios.AddSingleton<AuditorPeticiones>();
        servicios.AddSingleton<PresupuestoDiario>();
        servicios.AddSingleton<LimitadorIdentidades>();
        servicios.AddSingleton(proveedor => new LimitadorTasaPorIp(
            proveedor.GetRequiredService<LimitesAsistente>().PreguntasPorMinutoPorIp,
            proveedor.GetRequiredService<TimeProvider>()));
        servicios.AddSingleton(proveedor => ConfiguracionDelAsistente(proveedor, configuracion));
        servicios.AddScoped<PreguntarAsistente>();
        servicios.AddScoped<ProcesadorPregunta>();
    }

    private static void AgregarInfraestructura(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton(proveedor => new ArchivoAuditoriaDiario(
            configuracion[ArchivoAuditoriaDiario.ClaveRuta],
            proveedor.GetRequiredService<IReloj>()));
        servicios.AddSingleton<IAuditoria, AuditoriaJsonConsola>();
        servicios.AddSingleton<IBaseConocimiento>(proveedor =>
            CargadorConocimiento.Cargar(proveedor.GetRequiredService<IOptions<OpcionesConocimiento>>().Value, UmbralesDeEntregaDe(configuracion)));
        servicios.AddHttpClient<IApiAcademica, ClienteApiAcademica>((proveedor, cliente) =>
            {
                var opciones = proveedor.GetRequiredService<IOptions<OpcionesApiAcademica>>().Value;
                cliente.BaseAddress = new Uri(opciones.UrlBase.TrimEnd('/') + "/");
                cliente.Timeout = TimeSpan.FromSeconds(proveedor.GetRequiredService<LimitesAsistente>().PresupuestoApiAcademicaSegundos);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
    }

    private static void AgregarLlm(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton<IVariablesEntorno, VariablesEntornoProceso>();
        servicios.AddSingleton(proveedor => ResolutorProveedorLlm.Resolver(
            EntornoDesde(proveedor.GetRequiredService<IVariablesEntorno>(), configuracion),
            proveedor.GetRequiredService<IOptions<OpcionesLlm>>().Value));
        servicios.AddHttpClient(NombreClienteLlm)
            .ConfigureHttpClient((proveedor, cliente) =>
                cliente.Timeout = TimeSpan.FromSeconds(proveedor.GetRequiredService<LimitesAsistente>().TimeoutLlmSegundos));
        servicios.AddSingleton<IHttpMessageHandlerBuilderFilter>(new SinRedireccionesLlm(NombreClienteLlm));
        servicios.AddScoped<IClienteLlm>(proveedor => FabricaClienteLlm.Crear(
            proveedor.GetRequiredService<ProveedorResuelto>(),
            proveedor.GetRequiredService<IVariablesEntorno>().Leer(VariableLlaveLlm),
            proveedor.GetRequiredService<IHttpClientFactory>().CreateClient(NombreClienteLlm)));
    }

    private static EntornoLlm EntornoDesde(IVariablesEntorno variables, IConfiguration configuracion)
    {
        string? Valor(string nombre) => variables.Leer(nombre) ?? configuracion[nombre];
        return new EntornoLlm(
            variables.Leer(VariableLlaveLlm),
            Valor("LLM_PROVEEDOR"),
            Valor("LLM_URL_BASE"),
            Valor("LLM_MODELO"),
            LeerDecimal(Valor("LLM_PRECIO_ENTRADA")),
            LeerDecimal(Valor("LLM_PRECIO_SALIDA")));
    }

    private static Action<string> AdvertirPor(ILogger logger) =>
        mensaje => ValidacionDeArranque.RegistrarAdvertencia(logger, mensaje);

    private static decimal? LeerDecimal(string? valor) =>
        decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) ? numero : null;

    private static ConfiguracionAsistente ConfiguracionDelAsistente(IServiceProvider proveedor, IConfiguration configuracion)
    {
        var resuelto = proveedor.GetRequiredService<ProveedorResuelto>();
        var precio = resuelto.PrecioDesconocido ? null : new PrecioModelo(resuelto.PrecioEntradaPorMillon, resuelto.PrecioSalidaPorMillon);
        return new ConfiguracionAsistente(
            precio,
            configuracion[ClaveMarcadorCanario],
            null,
            ValidadorDeConfiguracion.Efectivo(configuracion, ClaveReservaPorPeticion),
            Entero(configuracion, ClaveRelevanciaMinima, ConfiguracionAsistente.RelevanciaMinimaPorDefecto),
            Entero(configuracion, ClaveApoyoMinimoRespaldo, ConfiguracionAsistente.ApoyoMinimoPorDefecto),
            configuracion.GetSection(SeccionAsignaturas).Get<AsignaturaCatalogo[]>(),
            ValidadorDeConfiguracion.Efectivo(configuracion, ClaveReservaSinPrecio) ?? ConfiguracionAsistente.ReservaSinPrecioPorDefectoUsd,
            AdvertirPor(proveedor.GetRequiredService<ILoggerFactory>().CreateLogger("TutorFlow.Configuracion")),
            AjustesDelModelo(configuracion));
    }

    private static UmbralesDeEntrega UmbralesDeEntregaDe(IConfiguration configuracion) =>
        new(
            Entero(configuracion, ClavePuntajeRelativoMinimo, PuntajeRelativoMinimoPorDefecto),
            Entero(configuracion, ClavePuntajeRelativoRelevante, PuntajeRelativoRelevantePorDefecto),
            Entero(configuracion, ClaveRelevanciaMinima, ConfiguracionAsistente.RelevanciaMinimaPorDefecto));

    internal static AjustesDelModelo AjustesDelModelo(IConfiguration configuracion) =>
        new(
            ValidadorDeConfiguracion.Efectivo(configuracion, ClaveTemperatura) ?? 0m,
            Entero(configuracion, ClaveSemilla, SemillaPorDefecto));

    private static int Entero(IConfiguration configuracion, string clave, int porDefecto) =>
        (int?)ValidadorDeConfiguracion.Efectivo(configuracion, clave) ?? porDefecto;
}
