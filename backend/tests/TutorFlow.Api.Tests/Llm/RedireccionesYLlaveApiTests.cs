using Microsoft.AspNetCore.Http;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Llm;

public sealed class RedireccionesLlmApiTests
{
    private const string LlaveDeLaPrueba = "sk-proj-LLAVE-REDIRECCION-9911";

    public static TheoryData<int> Redirecciones => new() { 301, 302, 307, 308 };

    [Theory]
    [MemberData(nameof(Redirecciones))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-160")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_160_Una_redireccion_del_proveedor_es_503_y_no_se_llama_a_location(int codigo)
    {
        await using var destino = await ServidorLocal.IniciarAsync(contexto => contexto.Response.WriteAsync("{}"));
        await using var proveedor = await ServidorLocal.IniciarAsync(contexto =>
        {
            contexto.Response.StatusCode = codigo;
            contexto.Response.Headers.Location = $"{destino.Url}/destino";
            return Task.CompletedTask;
        });
        var configuracion = new Dictionary<string, string?>
        {
            ["LLM_API_KEY"] = LlaveDeLaPrueba,
            ["LLM_PROVEEDOR"] = "openai-compatible",
            ["LLM_URL_BASE"] = proveedor.Url
        };
        using var fabrica = new FabricaApi(configuracion: configuracion, reemplazarLlm: false);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.True(proveedor.Peticiones >= 1, "el proveedor doble no recibió la petición");
        Assert.Equal(0, destino.Peticiones);
        Assert.Empty(destino.Autorizaciones);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-160")]
    [Trait("SEG", "SEG-07")]
    public async Task ESC_160_El_503_por_redireccion_se_audita_como_error_y_no_expone_la_location()
    {
        await using var destino = await ServidorLocal.IniciarAsync(contexto => contexto.Response.WriteAsync("{}"));
        await using var proveedor = await ServidorLocal.IniciarAsync(contexto =>
        {
            contexto.Response.StatusCode = 302;
            contexto.Response.Headers.Location = $"{destino.Url}/destino";
            return Task.CompletedTask;
        });
        var configuracion = new Dictionary<string, string?>
        {
            ["LLM_API_KEY"] = LlaveDeLaPrueba,
            ["LLM_PROVEEDOR"] = "openai-compatible",
            ["LLM_URL_BASE"] = proveedor.Url
        };
        using var fabrica = new FabricaApi(configuracion: configuracion, reemplazarLlm: false);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es mi promedio?");

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
        Assert.DoesNotContain(destino.Url, respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveDeLaPrueba, respuesta.Texto, StringComparison.Ordinal);
    }
}

public sealed class LlaveSoloDelEntornoApiTests
{
    private const string LlaveEnConfiguracion = "sk-ant-canaria-appsettings";
    private const string HostDeOllama = "host.docker.internal";

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-161")]
    [Trait("SEG", "SEG-11")]
    public async Task ESC_161_Una_llave_en_la_configuracion_se_ignora_y_el_proveedor_resuelto_es_ollama()
    {
        Assert.Null(Environment.GetEnvironmentVariable("LLM_API_KEY"));
        var manejador = new ManejadorLlmFalso((peticion, _) => RespuestasProveedor.Texto(peticion, "Respuesta de prueba.", 10, 5));
        var configuracion = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveEnConfiguracion };
        using var fabrica = new FabricaApi(configuracion: configuracion, reemplazarLlm: false, manejadorHttp: manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(200, respuesta.Estado);
        var peticion = Assert.Single(manejador.Peticiones);
        Assert.Equal(HostDeOllama, peticion.Uri.Host);
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-161")]
    [Trait("SEG", "SEG-11")]
    public async Task ESC_161_Ninguna_peticion_saliente_log_ni_respuesta_contiene_la_llave_de_la_configuracion()
    {
        Assert.Null(Environment.GetEnvironmentVariable("LLM_API_KEY"));
        var manejador = new ManejadorLlmFalso((peticion, _) => RespuestasProveedor.Texto(peticion, "Respuesta de prueba.", 10, 5));
        var configuracion = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveEnConfiguracion };
        using var fabrica = new FabricaApi(configuracion: configuracion, reemplazarLlm: false, manejadorHttp: manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.NotEmpty(manejador.Peticiones);
        Assert.All(manejador.Peticiones, p =>
        {
            Assert.DoesNotContain(LlaveEnConfiguracion, p.Cuerpo, StringComparison.Ordinal);
            Assert.DoesNotContain(LlaveEnConfiguracion, p.Uri.ToString(), StringComparison.Ordinal);
            Assert.All(p.Encabezados.Values, valor => Assert.DoesNotContain(LlaveEnConfiguracion, valor, StringComparison.Ordinal));
        });
        Assert.DoesNotContain(LlaveEnConfiguracion, respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains(LlaveEnConfiguracion, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-161")]
    [Trait("SEG", "SEG-11")]
    public async Task ESC_161_El_log_de_arranque_registra_ollama_y_no_el_proveedor_de_la_llave_ignorada()
    {
        var configuracion = new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveEnConfiguracion };
        using var fabrica = new FabricaApi(configuracion: configuracion);

        await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains("ollama", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("anthropic", StringComparison.OrdinalIgnoreCase));
    }
}
