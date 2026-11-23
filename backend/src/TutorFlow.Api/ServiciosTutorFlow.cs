using System.Globalization;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using TutorFlow.Core.Asistente;
using TutorFlow.Core.Autorizacion;
using TutorFlow.Infrastructure.ApiAcademica;
using TutorFlow.Infrastructure.Auditoria;
using TutorFlow.Infrastructure.Conocimiento;
using TutorFlow.Infrastructure.Llm;
using TutorFlow.Infrastructure.Tiempo;

namespace TutorFlow.Api;

public static class ServiciosTutorFlow
{
    private const string NombreClienteLlm = "llm";
    private const string SeccionCodigosAsignatura = "Autorizacion:CodigosAsignatura";
    private const string ClaveMarcadorCanario = "Llm:MarcadorCanario";
    private const string ClaveSeccionLimites = "Limites";

    public static IServiceCollection AgregarTutorFlow(this IServiceCollection servicios, IConfiguration configuracion)
    {
        AgregarOpciones(servicios);
        AgregarNucleo(servicios, configuracion);
        AgregarInfraestructura(servicios);
        AgregarLlm(servicios, configuracion);
        servicios.AddProblemDetails();
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
            .Validate(o => o.Proveedores.Count > 0, "Llm:Proveedores no puede estar vacio")
            .ValidateOnStart();
        servicios.AddOptions<LimitesAsistente>()
            .BindConfiguration(ClaveSeccionLimites)
            .Validate(l => l.MaximoCaracteresPregunta > 0 && l.PreguntasPorMinutoPorIp > 0, "Limites invalidos")
            .ValidateOnStart();
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
            TimeSpan.FromMinutes(1),
            TimeProvider.System));
        servicios.AddSingleton(proveedor => ConfiguracionDelAsistente(proveedor, configuracion));
        servicios.AddScoped<PreguntarAsistente>();
        servicios.AddScoped<ProcesadorPregunta>();
    }

    private static void AgregarInfraestructura(IServiceCollection servicios)
    {
        servicios.AddSingleton<IReloj, RelojSistema>();
        servicios.AddSingleton<IAuditoria, AuditoriaJsonConsola>();
        servicios.AddSingleton<IBaseConocimiento>(proveedor =>
            CargadorConocimiento.Cargar(proveedor.GetRequiredService<IOptions<OpcionesConocimiento>>().Value));
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
        servicios.AddSingleton(proveedor => ResolutorProveedorLlm.Resolver(
            EntornoDesde(configuracion),
            proveedor.GetRequiredService<IOptions<OpcionesLlm>>().Value));
        servicios.AddHttpClient(NombreClienteLlm)
            .ConfigureHttpClient((proveedor, cliente) =>
                cliente.Timeout = TimeSpan.FromSeconds(proveedor.GetRequiredService<LimitesAsistente>().TimeoutLlmSegundos));
        servicios.AddScoped<IClienteLlm>(proveedor => FabricaClienteLlm.Crear(
            proveedor.GetRequiredService<ProveedorResuelto>(),
            configuracion["LLM_API_KEY"],
            proveedor.GetRequiredService<IHttpClientFactory>().CreateClient(NombreClienteLlm)));
    }

    private static EntornoLlm EntornoDesde(IConfiguration configuracion) => new(
        configuracion["LLM_API_KEY"],
        configuracion["LLM_PROVEEDOR"],
        configuracion["LLM_URL_BASE"],
        configuracion["LLM_MODELO"],
        LeerDecimal(configuracion["LLM_PRECIO_ENTRADA"]),
        LeerDecimal(configuracion["LLM_PRECIO_SALIDA"]));

    private static decimal? LeerDecimal(string? valor) =>
        decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) ? numero : null;

    private static ConfiguracionAsistente ConfiguracionDelAsistente(IServiceProvider proveedor, IConfiguration configuracion)
    {
        var resuelto = proveedor.GetRequiredService<ProveedorResuelto>();
        var precio = resuelto.PrecioDesconocido ? null : new PrecioModelo(resuelto.PrecioEntradaPorMillon, resuelto.PrecioSalidaPorMillon);
        var codigos = configuracion.GetSection(SeccionCodigosAsignatura).Get<string[]>() ?? [];
        return new ConfiguracionAsistente(precio, configuracion[ClaveMarcadorCanario], codigos);
    }
}
