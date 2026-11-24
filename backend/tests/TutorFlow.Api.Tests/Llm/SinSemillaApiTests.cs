using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Llm;

public sealed class SinSemillaApiTests
{
    private const string Normativa = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string RespuestaNormativa = "Una asignatura se aprueba con 3.0.";
    private const string PreguntaConHerramienta = "¿Qué dice el reglamento sobre la habilitación?";
    private const string PreguntaConReintento = "¿Qué dice el reglamento sobre la habilitación?";
    private const string RespuestaQueFuerzaElReintento = "Tu promedio es 4.40.";
    private const string PropiedadTemperatura = "temperature";
    private const string PropiedadSemilla = "seed";
    private const string SemillaConfigurada = "42";

    public static TheoryData<string?, string> Proveedores => new()
    {
        { null, "ollama" },
        { "sk-LLAVE-ABC123-openai", "openai" },
        { "sk-ant-LLAVE-ABC123", "anthropic" },
        { "AIzaLLAVEABC123", "gemini" }
    };

    private static Dictionary<string, string?> Entorno(string? llave) =>
        llave is null ? [] : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave };

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador, string? semilla = null)
    {
        var configuracion = FabricaApi.Precios("1.0", "5.0");
        if (semilla is not null)
        {
            configuracion["Llm:Semilla"] = semilla;
        }

        return new FabricaApi(configuracion: configuracion, variablesEntorno: Entorno(llave), reemplazarLlm: false, manejadorHttp: manejador);
    }

    private static ManejadorLlmFalso ConUnaRespuesta() =>
        new((p, _) => RespuestasProveedor.Texto(p, RespuestaNormativa, 5));

    private static ManejadorLlmFalso ConHerramienta() =>
        new((p, n) => n == 0 ? RespuestasProveedor.LlamadaAHerramienta(p, 5) : RespuestasProveedor.Texto(p, "Datos revisados.", 5));

    private static ManejadorLlmFalso ConReintento() =>
        new((p, _) => RespuestasProveedor.Texto(p, RespuestaQueFuerzaElReintento, 5));

    private static JsonElement Cuerpo(PeticionLlm peticion) => JsonElement.Parse(peticion.Cuerpo);

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-85")]
    public async Task ESC_192abcd_Ningun_proveedor_recibe_semilla_y_la_temperatura_por_defecto_es_0(string? llave, string proveedor)
    {
        Assert.NotEmpty(proveedor);
        var manejador = ConUnaRespuesta();
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Normativa);

        Assert.Equal(200, respuesta.Estado);
        var cuerpo = Cuerpo(Assert.Single(manejador.Peticiones));
        Assert.Equal(0, cuerpo.GetProperty(PropiedadTemperatura).GetDouble());
        Assert.False(cuerpo.TryGetProperty(PropiedadSemilla, out _));
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-85")]
    public async Task ESC_192abcd_Toda_peticion_del_ciclo_de_herramientas_lleva_temperatura_0_y_ninguna_semilla(string? llave, string proveedor)
    {
        Assert.NotEmpty(proveedor);
        var manejador = ConHerramienta();
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConHerramienta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(2, manejador.Peticiones.Count);
        Assert.All(manejador.Peticiones, p =>
        {
            Assert.Equal(0, Cuerpo(p).GetProperty(PropiedadTemperatura).GetDouble());
            Assert.False(Cuerpo(p).TryGetProperty(PropiedadSemilla, out _));
        });
    }

    [Theory]
    [MemberData(nameof(Proveedores))]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-192")]
    [Trait("D", "D-85")]
    public async Task ESC_192h_Con_Llm_Semilla_configurada_arranca_sin_avisos_y_ninguna_peticion_incluida_la_del_reintento_lleva_semilla(string? llave, string proveedor)
    {
        Assert.NotEmpty(proveedor);
        var manejador = ConReintento();
        using var fabrica = Fabrica(llave, manejador, SemillaConfigurada);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaConReintento);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(2, manejador.Peticiones.Count);
        Assert.All(manejador.Peticiones, p => Assert.False(Cuerpo(p).TryGetProperty(PropiedadSemilla, out _)));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(ResultadoArranque.PrefijoInvalida, StringComparison.Ordinal) || m.Contains(ResultadoArranque.PrefijoIgnorada, StringComparison.Ordinal));
    }
}
