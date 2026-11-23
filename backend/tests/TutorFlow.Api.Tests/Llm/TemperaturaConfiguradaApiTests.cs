using System.Text.Json;

namespace TutorFlow.Api.Tests.Llm;

public sealed class TemperaturaConfiguradaApiTests
{
    private const string Normativa = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string PropiedadTemperatura = "temperature";
    private const string PropiedadSemilla = "seed";
    private const string LlaveDeOpenAi = "sk-LLAVE-ABC123-openai";
    private const string LlaveDeAnthropic = "sk-ant-LLAVE-ABC123";

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador, string temperatura)
    {
        var configuracion = FabricaApi.Precios("1.0", "5.0");
        configuracion["Llm:Temperatura"] = temperatura;
        var entorno = llave is null ? new Dictionary<string, string?>() : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave };
        return new FabricaApi(configuracion: configuracion, variablesEntorno: entorno, reemplazarLlm: false, manejadorHttp: manejador);
    }

    private static JsonElement Cuerpo(PeticionLlm peticion) => JsonElement.Parse(peticion.Cuerpo);

    private static ManejadorLlmFalso ConUnaRespuesta() =>
        new((p, _) => RespuestasProveedor.Texto(p, "Una asignatura se aprueba con 3.0.", 10, 5));

    [Theory]
    [InlineData(null, "0.3", true, "e")]
    [InlineData(LlaveDeAnthropic, "0.1", false, "f")]
    [InlineData(LlaveDeOpenAi, "0.2", true, "g")]
    [Trait("CA", "CA-20")]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-59")]
    public async Task ESC_192efg_La_temperatura_configurada_llega_al_proveedor_y_la_semilla_solo_a_los_compatibles_con_OpenAI(string? llave, string temperatura, bool llevaSemilla, string caso)
    {
        Assert.NotEmpty(caso);
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica(llave, manejador, temperatura);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        var cuerpo = Cuerpo(Assert.Single(manejador.Peticiones));
        Assert.Equal(double.Parse(temperatura, System.Globalization.CultureInfo.InvariantCulture), cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
        Assert.Equal(llevaSemilla, cuerpo.TryGetProperty(PropiedadSemilla, out var semilla));
        if (llevaSemilla)
        {
            Assert.Equal(1, semilla.GetInt64());
        }
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-59")]
    public async Task ESC_199f_Con_ollama_y_temperatura_0_2_el_log_de_arranque_la_registra_y_toda_peticion_incluida_la_del_reintento_la_lleva_con_semilla_1()
    {
        var manejador = new ManejadorLlmFalso((p, _) => RespuestasProveedor.Texto(p, "Tu promedio es 4.40.", 10, 5));
        using var fabrica = Fabrica(null, manejador, "0.2");

        await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedio);

        Assert.Equal(2, manejador.Peticiones.Count);
        Assert.All(manejador.Peticiones, peticion =>
        {
            var cuerpo = Cuerpo(peticion);
            Assert.Equal(0.2, cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
            Assert.Equal(1, cuerpo.GetProperty(PropiedadSemilla).GetInt64());
        });
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("0.2", StringComparison.Ordinal) && (m.Contains("emperatura", StringComparison.Ordinal) || m.Contains("emperature", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-59")]
    public async Task ESC_192_Sin_configurar_la_temperatura_el_defecto_es_0_en_el_cuerpo_y_en_el_log_de_arranque()
    {
        var manejador = ConUnaRespuesta();
        var configuracion = FabricaApi.Precios("1.0", "5.0");
        using var fabrica = new FabricaApi(configuracion: configuracion, variablesEntorno: new Dictionary<string, string?>(), reemplazarLlm: false, manejadorHttp: manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(0, Cuerpo(Assert.Single(manejador.Peticiones)).GetProperty(PropiedadTemperatura).GetDouble());
        Assert.Contains(fabrica.Logs.Mensajes, m => (m.Contains("emperatura", StringComparison.Ordinal) || m.Contains("emperature", StringComparison.Ordinal)) && m.Contains('0', StringComparison.Ordinal));
    }
}
