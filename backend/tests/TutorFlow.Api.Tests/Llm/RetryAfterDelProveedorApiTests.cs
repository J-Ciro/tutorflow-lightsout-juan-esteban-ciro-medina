using System.Net;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Llm;

public sealed class RetryAfterDelProveedorApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string LlaveAnthropic = "sk-ant-LLAVE-ABC123";
    private const string LlaveOpenAi = "sk-proj-LLAVE-ABC123";
    private const string CuerpoDelProveedor = """{"error":{"message":"rate limited"}}""";

    private static readonly DateTimeOffset Ahora = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, string, string> RetryAfterCopiados => new()
    {
        { LlaveAnthropic, "30", "30" },
        { LlaveOpenAi, "30", "30" },
        { LlaveAnthropic, "Mon, 23 Nov 2026 10:02:00 GMT", "120" },
        { LlaveOpenAi, "Mon, 23 Nov 2026 10:02:00 GMT", "120" },
        { LlaveAnthropic, "3600", "3600" },
        { LlaveOpenAi, "3600", "3600" },
        { LlaveAnthropic, "1", "1" },
        { LlaveOpenAi, "1", "1" },
        { LlaveAnthropic, "Mon, 23 Nov 2026 11:00:00 GMT", "3600" },
        { LlaveOpenAi, "Mon, 23 Nov 2026 10:00:01 GMT", "1" }
    };

    public static TheoryData<string, string?> RetryAfterNoCopiados => new()
    {
        { LlaveAnthropic, "0" },
        { LlaveAnthropic, "3601" },
        { LlaveAnthropic, "abc" },
        { LlaveAnthropic, "-5" },
        { LlaveAnthropic, "Mon, 23 Nov 2026 09:59:00 GMT" },
        { LlaveAnthropic, "Mon, 23 Nov 2026 12:00:00 GMT" },
        { LlaveAnthropic, null },
        { LlaveOpenAi, "0" },
        { LlaveOpenAi, "3601" },
        { LlaveOpenAi, "abc" },
        { LlaveOpenAi, "-5" },
        { LlaveOpenAi, "Mon, 23 Nov 2026 09:59:00 GMT" },
        { LlaveOpenAi, "Mon, 23 Nov 2026 12:00:00 GMT" },
        { LlaveOpenAi, null }
    };

    private static ManejadorLlmFalso ProveedorQueLimita(string? retryAfter) =>
        new((_, _) =>
        {
            var respuesta = RespuestasProveedor.Json(CuerpoDelProveedor, HttpStatusCode.TooManyRequests);
            if (retryAfter is not null)
            {
                respuesta.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
            }

            return respuesta;
        });

    private static FabricaApi FabricaConProveedor(string llave, ManejadorLlmFalso manejador)
    {
        var fabrica = new FabricaApi(variablesEntorno: new Dictionary<string, string?> { ["LLM_API_KEY"] = llave }, reemplazarLlm: false, manejadorHttp: manejador);
        fabrica.Reloj.Fijar(Ahora);
        return fabrica;
    }

    private static Dictionary<string, string> CuerpoSinIdentificadores(RespuestaApi respuesta) =>
        respuesta.Json.EnumerateObject()
            .Where(p => p.Name is not ("traceId" or "correlationId"))
            .ToDictionary(p => p.Name, p => p.Value.ToString());

    [Theory]
    [MemberData(nameof(RetryAfterCopiados))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-70")]
    public async Task ESC_110bce_Un_429_del_proveedor_da_503_con_su_retry_after_dentro_del_rango(string llave, string delProveedor, string esperado)
    {
        var manejador = ProveedorQueLimita(delProveedor);
        using var fabrica = FabricaConProveedor(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.Equal(esperado, respuesta.Encabezado("Retry-After"));
        Assert.Single(manejador.Peticiones);
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(RetryAfterNoCopiados))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-70")]
    public async Task ESC_110d_Un_429_con_retry_after_invalido_fuera_de_rango_o_ausente_da_503_sin_retry_after(string llave, string? delProveedor)
    {
        var manejador = ProveedorQueLimita(delProveedor);
        using var fabrica = FabricaConProveedor(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.False(respuesta.TieneEncabezado("Retry-After"));
        Assert.Single(manejador.Peticiones);
    }

    [Theory]
    [InlineData(LlaveAnthropic)]
    [InlineData(LlaveOpenAi)]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-70")]
    public async Task ESC_110b_El_cuerpo_del_503_por_429_es_igual_al_de_cualquier_otro_503_y_no_filtra_al_proveedor(string llave)
    {
        using var fabrica = FabricaConProveedor(llave, ProveedorQueLimita("30"));
        using var otra = new FabricaApi(llm: ClienteLlmDoble.Falla(new HttpRequestException("proveedor caido")));

        var porLimite = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        var porCaida = await otra.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(CuerpoSinIdentificadores(porCaida), CuerpoSinIdentificadores(porLimite));
        Assert.DoesNotContain("rate limited", porLimite.Texto, StringComparison.OrdinalIgnoreCase);
    }
}
