using System.Net;
using Microsoft.Extensions.Logging;
using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Llm;

public sealed class ErrorDelProveedorApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";
    private const string LlaveDeGemini = "AIzaCANARIA-llave-que-no-debe-aparecer";
    private const string CuerpoDeGemini = """{"error":{"code":400,"status":"INVALID_ARGUMENT","message":"Unknown name \"seed\": Cannot find field."}}""";
    private const string MensajeConDatos = "nota de prueba 4.39\\r\\nERROR falso";
    private const string LlaveEnElMensaje = "sk-ant-canaria";
    private const int LongitudMaximaDelCodigo = 64;
    private const string LlaveDeAnthropic = "sk-ant-api03-CANARIA-llave-que-no-debe-aparecer";
    private const string CuerpoDeAnthropic = """{"type":"error","error":{"type":"invalid_request_error","message":"seed: Extra inputs are not permitted"}}""";
    private const string CuerpoSoloConMensaje = """{"error":{"message":"x"}}""";
    private const string CuerpoQueNoEsJson = "<html>fallo del proveedor</html>";

    public static TheoryData<int, string, string> ErroresConDatosEnElMensaje => new()
    {
        { 400, $$$"""{"error":{"code":400,"status":"INVALID_ARGUMENT","message":"{{{MensajeConDatos}}}"}}""", "INVALID_ARGUMENT" },
        { 401, $$$"""{"error":{"code":401,"status":"UNAUTHENTICATED","message":"Clave incorrecta {{{LlaveEnElMensaje}}}-9911"}}""", "UNAUTHENTICATED" },
        { 500, $$$"""{"error":{"code":"server_error","message":"fallo con {{{LlaveEnElMensaje}}} y {{{MensajeConDatos}}}"}}""", "server_error" }
    };

    public static TheoryData<string, int, string, string?> ErroresD85 => new()
    {
        { LlaveDeAnthropic, 400, CuerpoDeAnthropic, "400 invalid_request_error" },
        { LlaveDeGemini, 500, CuerpoSoloConMensaje, null },
        { LlaveDeGemini, 502, CuerpoQueNoEsJson, null }
    };

    private static FabricaApi Fabrica(ManejadorLlmFalso manejador) =>
        new(variablesEntorno: new Dictionary<string, string?> { ["LLM_API_KEY"] = LlaveDeGemini }, reemplazarLlm: false, manejadorHttp: manejador);

    private static ManejadorLlmFalso Responde(int estado, string cuerpo) =>
        new((_, _) => RespuestasProveedor.Json(cuerpo, (HttpStatusCode)estado));

    private static string TodoElLog(FabricaApi fabrica) => string.Join("\n", fabrica.Logs.Mensajes);

    [Theory]
    [MemberData(nameof(ErroresD85))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110h_El_503_es_generico_y_el_log_warn_trae_el_estado_y_el_codigo_solo_si_existe_sin_la_palabra_message(string llave, int estado, string cuerpo, string? esperado)
    {
        using var fabrica = new FabricaApi(variablesEntorno: new Dictionary<string, string?> { ["LLM_API_KEY"] = llave }, reemplazarLlm: false, manejadorHttp: Responde(estado, cuerpo));
        var textoEstado = estado.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        var aviso = Assert.Single(fabrica.Logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains(textoEstado, StringComparison.Ordinal));
        Assert.Contains(esperado ?? textoEstado, aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("message", TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain(llave, TodoElLog(fabrica), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(500, CuerpoSoloConMensaje)]
    [InlineData(502, CuerpoQueNoEsJson)]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110h_Sin_codigo_en_el_cuerpo_o_con_un_cuerpo_que_no_es_json_el_aviso_no_inventa_codigo(int estado, string cuerpo)
    {
        using var fabrica = Fabrica(Responde(estado, cuerpo));

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        var aviso = Assert.Single(fabrica.Logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains(estado.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Assert.DoesNotContain("INVALID_ARGUMENT", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("invalid_request_error", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("server_error", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("fallo del proveedor", TodoElLog(fabrica), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110f_Un_400_del_proveedor_deja_una_linea_warn_con_el_estado_y_el_codigo_y_nada_mas_del_cuerpo()
    {
        var manejador = Responde(400, CuerpoDeGemini);
        using var fabrica = Fabrica(manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        var avisos = fabrica.Logs.Entradas.Where(e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("INVALID_ARGUMENT", StringComparison.Ordinal)).ToList();
        var aviso = Assert.Single(avisos);
        Assert.Contains("400", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("Unknown name", TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain("Cannot find field", TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveDeGemini, TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveDeGemini, respuesta.Texto, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110f_El_log_no_contiene_encabezados_ni_el_cuerpo_de_la_peticion_al_proveedor()
    {
        var manejador = Responde(400, CuerpoDeGemini);
        using var fabrica = Fabrica(manejador);

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        var peticion = Assert.Single(manejador.Peticiones);
        Assert.DoesNotContain("Bearer", TodoElLog(fabrica), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chat/completions", TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain(peticion.Cuerpo[..Math.Min(80, peticion.Cuerpo.Length)], TodoElLog(fabrica), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ErroresConDatosEnElMensaje))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110g_Un_4xx_o_5xx_con_datos_o_la_llave_en_el_mensaje_da_503_generico_y_el_log_solo_trae_estado_y_codigo(int estado, string cuerpo, string codigo)
    {
        using var fabrica = Fabrica(Responde(estado, cuerpo));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        var log = TodoElLog(fabrica);
        Assert.Contains(estado.ToString(System.Globalization.CultureInfo.InvariantCulture), log, StringComparison.Ordinal);
        Assert.Contains(codigo, log, StringComparison.Ordinal);
        Assert.DoesNotContain("nota de prueba", log, StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveEnElMensaje, log, StringComparison.Ordinal);
        Assert.DoesNotContain(LlaveDeGemini, log, StringComparison.Ordinal);
        Assert.DoesNotContain(log.Split('\n'), linea => linea.TrimEnd('\r').StartsWith("ERROR falso", StringComparison.Ordinal));
        Assert.DoesNotContain("nota de prueba", respuesta.Texto, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110g_Un_cuerpo_que_no_es_json_deja_solo_el_estado_en_el_log()
    {
        using var fabrica = Fabrica(Responde(502, "<html>secreto academico 4.39</html>"));

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.Contains("502", TodoElLog(fabrica), StringComparison.Ordinal);
        Assert.DoesNotContain("secreto academico", TodoElLog(fabrica), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110_El_codigo_se_sanea_sin_saltos_de_linea_y_con_un_maximo_de_64_caracteres()
    {
        var codigoLargo = new string('A', 100);
        var cuerpo = $$$"""{"error":{"status":"{{{codigoLargo}}}\r\nERROR falso","message":"x"}}""";
        using var fabrica = Fabrica(Responde(400, cuerpo));

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        var avisos = fabrica.Logs.Entradas.Where(e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("AAAA", StringComparison.Ordinal)).ToList();
        var aviso = Assert.Single(avisos);
        Assert.DoesNotContain('\r', aviso.Mensaje);
        Assert.DoesNotContain('\n', aviso.Mensaje);
        Assert.DoesNotContain(new string('A', LongitudMaximaDelCodigo + 1), aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("ERROR falso", aviso.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    [Trait("D", "D-85")]
    public async Task ESC_110_El_codigo_solo_conserva_letras_digitos_guion_bajo_punto_y_guion()
    {
        var cuerpo = """{"error":{"status":"BAD CODE<script>$(x)|;_ok.1-2","message":"x"}}""";
        using var fabrica = Fabrica(Responde(400, cuerpo));

        await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        var aviso = Assert.Single(fabrica.Logs.Entradas, e => e.Nivel == LogLevel.Warning && e.Mensaje.Contains("_ok.1-2", StringComparison.Ordinal));
        Assert.DoesNotContain("<script>", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("$(x)", aviso.Mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("|;", aviso.Mensaje, StringComparison.Ordinal);
    }
}
