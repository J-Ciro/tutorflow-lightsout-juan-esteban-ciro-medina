using System.Text.Json;

namespace TutorFlow.Api.Tests.Llm;

public sealed class LlamadaDeterministaApiTests
{
    private const string Normativa = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string RespuestaNormativa = "Una asignatura se aprueba con 3.0.";
    private const string PreguntaConHerramienta = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PropiedadTemperatura = "temperature";
    private const string PropiedadSemilla = "seed";

    public static TheoryData<string?, string> ProveedoresCompatiblesConOpenAi => new()
    {
        { null, "ollama" },
        { "sk-LLAVE-ABC123-openai", "openai" },
        { "gsk_LLAVEABC123", "groq" },
        { "sk-or-LLAVE-ABC123", "openrouter" },
        { "AIzaLLAVEABC123", "gemini" }
    };

    public static TheoryData<string?, string, int> SemillasConfiguradas => new()
    {
        { null, "ollama", 42 },
        { "sk-LLAVE-ABC123-openai", "openai", 42 },
        { null, "ollama", 0 },
        { null, "ollama", int.MaxValue },
        { "gsk_LLAVEABC123", "groq", 7 }
    };

    private static Dictionary<string, string?> Entorno(string? llave) =>
        llave is null ? [] : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave };

    private static Dictionary<string, string?> Configuracion(string? semilla)
    {
        var configuracion = FabricaApi.Precios("1.0", "5.0");
        if (semilla is not null)
        {
            configuracion["Llm:Semilla"] = semilla;
        }

        return configuracion;
    }

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador, string? semilla = null) =>
        new(configuracion: Configuracion(semilla), variablesEntorno: Entorno(llave), reemplazarLlm: false, manejadorHttp: manejador);

    private static ManejadorLlmFalso ConUnaRespuesta() =>
        new((p, _) => RespuestasProveedor.Texto(p, RespuestaNormativa, 10, 5));

    private static ManejadorLlmFalso ConHerramienta() =>
        new((p, n) => n == 0 ? RespuestasProveedor.LlamadaAHerramienta(p, 10, 5) : RespuestasProveedor.Texto(p, "Datos revisados.", 10, 5));

    private static JsonElement Cuerpo(PeticionLlm peticion) => JsonElement.Parse(peticion.Cuerpo);

    [Theory]
    [MemberData(nameof(ProveedoresCompatiblesConOpenAi))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192ab_Los_adaptadores_compatibles_con_OpenAI_envian_temperatura_0_y_semilla_1(string? llave, string proveedor)
    {
        Assert.NotEmpty(proveedor);
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        var peticion = Assert.Single(manejador.Peticiones);
        var cuerpo = Cuerpo(peticion);
        Assert.Equal(0, cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
        Assert.Equal(1, cuerpo.GetProperty(PropiedadSemilla).GetInt64());
    }

    [Fact]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192c_Anthropic_envia_temperatura_0_y_ninguna_semilla()
    {
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica("sk-ant-LLAVE-ABC123", manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        var cuerpo = Cuerpo(Assert.Single(manejador.Peticiones));
        Assert.Equal(0, cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
        Assert.False(cuerpo.TryGetProperty(PropiedadSemilla, out _));
    }

    [Fact]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192c_Anthropic_no_envia_semilla_aunque_Llm_Semilla_este_configurada()
    {
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica("sk-ant-LLAVE-ABC123", manejador, "42");

        await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.False(Cuerpo(Assert.Single(manejador.Peticiones)).TryGetProperty(PropiedadSemilla, out _));
    }

    [Theory]
    [MemberData(nameof(SemillasConfiguradas))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192d_La_semilla_configurada_llega_al_proveedor(string? llave, string proveedor, int semilla)
    {
        Assert.NotEmpty(proveedor);
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica(llave, manejador, semilla.ToString(System.Globalization.CultureInfo.InvariantCulture));

        await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        var cuerpo = Cuerpo(Assert.Single(manejador.Peticiones));
        Assert.Equal(semilla, cuerpo.GetProperty(PropiedadSemilla).GetInt64());
        Assert.Equal(0, cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sk-LLAVE-ABC123-openai")]
    [InlineData("sk-ant-LLAVE-ABC123")]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192abc_Toda_peticion_del_ciclo_de_herramientas_lleva_temperatura_0(string? llave)
    {
        var manejador = ConHerramienta();
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConHerramienta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(2, manejador.Peticiones.Count);
        Assert.All(manejador.Peticiones, p => Assert.Equal(0, Cuerpo(p).GetProperty(PropiedadTemperatura).GetDouble()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sk-LLAVE-ABC123-openai")]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("RF", "D-47")]
    public async Task ESC_192ab_Toda_peticion_del_ciclo_de_herramientas_lleva_la_misma_semilla(string? llave)
    {
        var manejador = ConHerramienta();
        using var fabrica = Fabrica(llave, manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConHerramienta);

        Assert.All(manejador.Peticiones, p => Assert.Equal(1, Cuerpo(p).GetProperty(PropiedadSemilla).GetInt64()));
    }
}
