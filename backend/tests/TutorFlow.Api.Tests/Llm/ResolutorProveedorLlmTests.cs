using Microsoft.Extensions.Configuration;
using TutorFlow.Infrastructure.Llm;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Llm;

public sealed class ResolutorProveedorLlmTests
{
    private static readonly OpcionesLlm Opciones = CargarOpciones();

    public static TheoryData<string?, string?, string?, ProveedorLlm, AdaptadorLlm> Resoluciones => new()
    {
        { "sk-ant-api03-abc", null, null, ProveedorLlm.Anthropic, AdaptadorLlm.Anthropic },
        { "sk-or-v1-abc", null, null, ProveedorLlm.OpenRouter, AdaptadorLlm.CompatibleOpenAi },
        { "gsk_abc", null, null, ProveedorLlm.Groq, AdaptadorLlm.CompatibleOpenAi },
        { "AIzaSyAbc", null, null, ProveedorLlm.Gemini, AdaptadorLlm.CompatibleOpenAi },
        { "sk-proj-abc", null, null, ProveedorLlm.OpenAi, AdaptadorLlm.CompatibleOpenAi },
        { "sk-abc123", null, null, ProveedorLlm.OpenAi, AdaptadorLlm.CompatibleOpenAi },
        { null, null, null, ProveedorLlm.Ollama, AdaptadorLlm.CompatibleOpenAi },
        { "", null, null, ProveedorLlm.Ollama, AdaptadorLlm.CompatibleOpenAi },
        { "   ", null, null, ProveedorLlm.Ollama, AdaptadorLlm.CompatibleOpenAi },
        { "zzz-123", null, null, ProveedorLlm.Desconocido, AdaptadorLlm.NoConfigurado },
        { "zzz-123", null, "http://mi-proxy:8000/v1", ProveedorLlm.OpenAiCompatible, AdaptadorLlm.CompatibleOpenAi },
        { "sk-ant-abc", "groq", null, ProveedorLlm.Groq, AdaptadorLlm.CompatibleOpenAi },
        { "gsk_abc", "anthropic", null, ProveedorLlm.Anthropic, AdaptadorLlm.Anthropic },
        { "sk-abc", "openrouter", null, ProveedorLlm.OpenRouter, AdaptadorLlm.CompatibleOpenAi },
        { "sk-abc", "gemini", null, ProveedorLlm.Gemini, AdaptadorLlm.CompatibleOpenAi },
        { "sk-ant-abc", "openai", null, ProveedorLlm.OpenAi, AdaptadorLlm.CompatibleOpenAi },
        { "sk-abc", "ollama", null, ProveedorLlm.Ollama, AdaptadorLlm.CompatibleOpenAi },
        { null, "anthropic", null, ProveedorLlm.Anthropic, AdaptadorLlm.Anthropic },
        { "zzz-123", "openai-compatible", "http://x:1/v1", ProveedorLlm.OpenAiCompatible, AdaptadorLlm.CompatibleOpenAi }
    };

    private static OpcionesLlm CargarOpciones()
    {
        var ruta = Path.Combine(DatosReferencia.RutaRepositorio(), "backend", "src", "TutorFlow.Api", "appsettings.json");
        var configuracion = new ConfigurationBuilder().AddJsonFile(ruta).Build();
        return configuracion.GetSection(OpcionesLlm.Seccion).Get<OpcionesLlm>()!;
    }

    private static ProveedorResuelto Resolver(string? llave = null, string? proveedor = null, string? urlBase = null, string? modelo = null, decimal? precioEntrada = null, decimal? precioSalida = null) =>
        ResolutorProveedorLlm.Resolver(new EntornoLlm(llave, proveedor, urlBase, modelo, precioEntrada, precioSalida), Opciones);

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "RNF-05")]
    public void La_configuracion_trae_los_seis_proveedores()
    {
        Assert.Equal(["anthropic", "gemini", "groq", "ollama", "openai", "openrouter"], Opciones.Proveedores.Keys.Order().ToArray());
    }

    [Theory]
    [MemberData(nameof(Resoluciones))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    public void ESC_119_El_proveedor_se_resuelve_por_variable_explicita_y_luego_por_prefijo_de_la_llave(string? llave, string? proveedor, string? urlBase, ProveedorLlm esperado, AdaptadorLlm adaptador)
    {
        var resuelto = Resolver(llave, proveedor, urlBase);

        Assert.Equal(esperado, resuelto.Proveedor);
        Assert.Equal(adaptador, resuelto.Adaptador);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void Sin_llave_usa_ollama_con_qwen_y_costo_cero()
    {
        var resuelto = Resolver();

        Assert.Equal("qwen2.5:7b", resuelto.Modelo);
        Assert.Equal("http://host.docker.internal:11434", resuelto.UrlBase);
        Assert.Equal(0m, resuelto.PrecioEntradaPorMillon);
        Assert.Equal(0m, resuelto.PrecioSalidaPorMillon);
        Assert.False(resuelto.PrecioDesconocido);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void Anthropic_usa_haiku_con_los_precios_de_la_spec()
    {
        var resuelto = Resolver("sk-ant-abc");

        Assert.Equal("claude-haiku-5-5", resuelto.Modelo);
        Assert.Equal(0.10m, resuelto.PrecioEntradaPorMillon);
        Assert.Equal(0.50m, resuelto.PrecioSalidaPorMillon);
        Assert.False(resuelto.PrecioDesconocido);
    }

    [Theory]
    [InlineData("sk-ant-abc", "https://api.anthropic.com")]
    [InlineData("gsk_abc", "https://api.groq.com/openai/v1")]
    [InlineData("sk-or-abc", "https://openrouter.ai/api/v1")]
    [InlineData("sk-abc", "https://api.openai.com/v1")]
    [InlineData("AIzaabc", "https://generativelanguage.googleapis.com/v1beta/openai")]
    [Trait("ESC", "ESC-119")]
    public void Cada_proveedor_usa_su_url_base_por_defecto(string llave, string urlBase)
    {
        Assert.Equal(urlBase, Resolver(llave).UrlBase);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void La_url_base_explicita_gana_sobre_la_de_la_configuracion()
    {
        Assert.Equal("http://otra:1/v1", Resolver("sk-abc", urlBase: "http://otra:1/v1").UrlBase);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void Un_modelo_sin_precio_configurado_reporta_precio_desconocido_y_costo_cero()
    {
        var resuelto = Resolver("sk-abc", modelo: "modelo-sin-precio");

        Assert.Equal("modelo-sin-precio", resuelto.Modelo);
        Assert.True(resuelto.PrecioDesconocido);
        Assert.Equal(0m, resuelto.PrecioEntradaPorMillon);
        Assert.Equal(0m, resuelto.PrecioSalidaPorMillon);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void Los_precios_explicitos_sobrescriben_a_la_configuracion()
    {
        var resuelto = Resolver("sk-abc", modelo: "modelo-sin-precio", precioEntrada: 1.0m, precioSalida: 5.0m);

        Assert.Equal(1.0m, resuelto.PrecioEntradaPorMillon);
        Assert.Equal(5.0m, resuelto.PrecioSalidaPorMillon);
        Assert.False(resuelto.PrecioDesconocido);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void Un_modelo_conocido_del_proveedor_usa_su_precio_configurado()
    {
        var resuelto = Resolver("sk-abc", modelo: "gpt-4o-mini");

        Assert.Equal(0.15m, resuelto.PrecioEntradaPorMillon);
        Assert.Equal(0.60m, resuelto.PrecioSalidaPorMillon);
    }

    [Fact]
    [Trait("ESC", "ESC-119")]
    public void La_resolucion_nunca_incluye_la_llave()
    {
        var resuelto = Resolver("sk-ant-SECRETO-123");

        Assert.DoesNotContain("SECRETO", resuelto.ToString(), StringComparison.Ordinal);
    }
}

public sealed class ProveedorLlmLogDeArranqueTests
{
    [Theory]
    [InlineData("sk-ant-SECRETO-ARRANQUE", "claude-haiku-5-5")]
    [InlineData("gsk_SECRETO-ARRANQUE", "llama-3.1-8b-instant")]
    [InlineData("sk-SECRETO-ARRANQUE", "gpt-4o-mini")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    [Trait("SEG", "SEG-11")]
    public async Task ESC_119_El_log_de_arranque_muestra_el_modelo_y_nunca_la_llave(string llave, string modelo)
    {
        var entorno = new Dictionary<string, string?> { ["LLM_API_KEY"] = llave };
        using var fabrica = new FabricaApi(variablesEntorno: entorno);

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);
        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");

        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains(modelo, StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("SECRETO-ARRANQUE", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-119")]
    public async Task ESC_119_Sin_llave_el_log_de_arranque_muestra_ollama_y_su_modelo()
    {
        using var fabrica = new FabricaApi();

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("qwen2.5:7b", StringComparison.Ordinal));
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("ollama", StringComparison.OrdinalIgnoreCase));
    }
}
