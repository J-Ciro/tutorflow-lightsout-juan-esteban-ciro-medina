using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Llm;

public sealed class ProveedorNoDeterminableApiTests
{
    private const string LlaveDeGemini = "AIzaCANARIA-llave-no-debe-aparecer";
    private const string LlaveSinPrefijo = "zzz-CANARIA-llave-no-debe-aparecer";
    private const string LlaveGenerica = "sk-CANARIA-llave-no-debe-aparecer";
    private const string LineaSinProveedor = "No se pudo determinar el proveedor: define LLM_PROVEEDOR, LLM_MODELO o LLM_URL_BASE";
    private const string PreguntaConLlm = "¿Cuál es la nota mínima para aprobar una asignatura?";

    public static TheoryData<string?, string, string> ProveedoresPorPista => new()
    {
        { "sk-ant-CANARIA-llave", "Anthropic", "claude-haiku-5-5" },
        { "sk-or-CANARIA-llave", "OpenRouter", "openai/gpt-4o-mini" },
        { "gsk_CANARIA-llave", "Groq", "llama-3.1-8b-instant" },
        { "AIzaCANARIA-llave", "Gemini", "gemini" },
        { "sk-proj-CANARIA-llave", "OpenAI", "gpt-4o-mini" },
        { null, "Ollama", "qwen2.5:7b" }
    };

    private static Dictionary<string, string?> Entorno(string? llave, string? modelo = null, string? urlBase = null)
    {
        var entorno = new Dictionary<string, string?>();
        if (llave is not null)
        {
            entorno["LLM_API_KEY"] = llave;
        }

        if (modelo is not null)
        {
            entorno["LLM_MODELO"] = modelo;
        }

        if (urlBase is not null)
        {
            entorno["LLM_URL_BASE"] = urlBase;
        }

        return entorno;
    }

    private static ManejadorLlmFalso ProveedorQueResponde() =>
        new((p, _) => RespuestasProveedor.Texto(p, "Una asignatura se aprueba con 3.0.", 5));

    private static string Log(FabricaApi fabrica) => string.Join("\n", fabrica.Logs.Mensajes);

    [Theory]
    [MemberData(nameof(ProveedoresPorPista))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119a_El_log_de_arranque_muestra_el_proveedor_resuelto_por_la_pista_de_la_llave_y_su_modelo_pero_nunca_la_llave(string? llave, string proveedor, string fragmentoDelModelo)
    {
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(llave));

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains(proveedor, StringComparison.OrdinalIgnoreCase) && m.Contains(fragmentoDelModelo, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("CANARIA", Log(fabrica), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119d_Llave_de_Gemini_con_modelo_de_Anthropic_queda_NoConfigurado_con_un_log_que_nombra_ambos_sin_la_llave()
    {
        var manejador = ProveedorQueResponde();
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(LlaveDeGemini, "claude-haiku-5-5"), reemplazarLlm: false, manejadorHttp: manejador);

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var pregunta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConLlm);

        Assert.Equal(200, salud.Estado);
        Afirmaciones.ProblemDetails(pregunta, 503);
        Assert.Empty(manejador.Peticiones);
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("Gemini", StringComparison.OrdinalIgnoreCase) && m.Contains("Anthropic", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(LlaveDeGemini, Log(fabrica), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119e_Llave_generica_sk_con_modelo_gemini_resuelve_Gemini_y_responde()
    {
        var manejador = ProveedorQueResponde();
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(LlaveGenerica, "gemini-2.5-flash"), reemplazarLlm: false, manejadorHttp: manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConLlm);

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("generativelanguage", Assert.Single(manejador.Peticiones).Uri.Host, StringComparison.Ordinal);
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("Gemini", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119f_Llave_sin_prefijo_y_modelo_qwen_sin_url_base_registra_la_linea_exacta_y_no_infiere_por_qwen()
    {
        var manejador = ProveedorQueResponde();
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(LlaveSinPrefijo, "qwen2.5:7b"), reemplazarLlm: false, manejadorHttp: manejador);

        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var pregunta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConLlm);

        Assert.Equal(200, salud.Estado);
        Afirmaciones.ProblemDetails(pregunta, 503);
        Assert.Contains(LineaSinProveedor, fabrica.Logs.Mensajes.SelectMany(m => m.Split('\n')).Select(l => l.TrimEnd('\r')));
        Assert.Empty(manejador.Peticiones);
        Assert.DoesNotContain(LlaveSinPrefijo, Log(fabrica), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119g_Llave_sin_prefijo_y_solo_url_base_usa_un_servidor_compatible_con_OpenAI_en_esa_url()
    {
        var manejador = ProveedorQueResponde();
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(LlaveSinPrefijo, urlBase: "http://mi-proxy.local:8000/v1"), reemplazarLlm: false, manejadorHttp: manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConLlm);

        Assert.Equal(200, respuesta.Estado);
        var peticion = Assert.Single(manejador.Peticiones);
        Assert.Equal("mi-proxy.local", peticion.Uri.Host);
        Assert.StartsWith("/v1/", peticion.Uri.AbsolutePath, StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveSinPrefijo, Log(fabrica), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AIzaCANARIA-llave-no-debe-aparecer", "claude-haiku-5-5", null)]
    [InlineData("zzz-CANARIA-llave-no-debe-aparecer", "qwen2.5:7b", null)]
    [InlineData("zzz-CANARIA-llave-no-debe-aparecer", null, "http://mi-proxy.local:8000/v1")]
    [InlineData("sk-CANARIA-llave-no-debe-aparecer", "gemini-2.5-flash", null)]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-11")]
    [Trait("ESC", "ESC-119")]
    [Trait("D", "D-84")]
    public async Task ESC_119h_La_llave_no_aparece_en_el_log_de_arranque_ni_en_los_errores(string llave, string? modelo, string? urlBase)
    {
        var manejador = ProveedorQueResponde();
        using var fabrica = new FabricaApi(variablesEntorno: Entorno(llave, modelo, urlBase), reemplazarLlm: false, manejadorHttp: manejador);

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        var pregunta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConLlm);

        Assert.DoesNotContain("CANARIA", Log(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain("CANARIA", pregunta.Texto, StringComparison.Ordinal);
    }
}
