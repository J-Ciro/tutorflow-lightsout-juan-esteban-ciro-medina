using TutorFlow.Pruebas.Soporte;
using System.Text.Json;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Llm;

public sealed class ContextoDeOllamaApiTests
{
    private const decimal CaracteresPorToken = 3.5m;
    private const decimal ProporcionMinimaDelContexto = 0.75m;
    private const decimal ProporcionRealMaxima = 1.04m;
    private const int TopeDeTokensEstimados = 3000;
    private const string Pregunta = "¿Cuál es mi promedio ponderado del periodo?";
    private const string Respuesta = "Tu promedio es 4.39.";
    private const string LlaveDeOpenAi = "sk-LLAVE-ABC123-openai";

    public static TheoryData<string, string?, int, bool> ReportesDelProveedor => new()
    {
        { "e2", null, 0, false },
        { "e3", null, 1, false },
        { "e4", null, 2, false }
    };

    private static int Reportado(int estimado, int caso) =>
        caso switch
        {
            0 => (int)Math.Ceiling(ProporcionMinimaDelContexto * estimado),
            1 => (int)Math.Floor(ProporcionRealMaxima * estimado),
            _ => 0
        };

    private static HttpResponseMessage SinUso(string texto) =>
        RespuestasProveedor.Json(JsonSerializer.Serialize(new
        {
            id = "c1",
            @object = "chat.completion",
            choices = new object[] { new { index = 0, message = new { role = "assistant", content = texto, refusal = (string?)null }, finish_reason = "stop" } }
        }));

    private static FabricaApi Fabrica(string? llave, ManejadorLlmFalso manejador) =>
        new(
            configuracion: FabricaApi.Precios("1.0", "5.0"),
            variablesEntorno: llave is null ? new Dictionary<string, string?>() : new Dictionary<string, string?> { ["LLM_API_KEY"] = llave },
            reemplazarLlm: false,
            manejadorHttp: manejador,
            conocimientoReal: true);

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199e1_Un_prompt_tokens_menor_que_el_75_por_ciento_de_la_estimacion_da_TEXTO_NO_VERIFICABLE_sin_reintento_y_contexto_truncado()
    {
        var estimado = 0;
        var reportado = 0;
        var manejador = new ManejadorLlmFalso((peticion, _) =>
        {
            estimado = ConsumoRealistaHttp.Estimados(peticion);
            reportado = (int)Math.Ceiling(ProporcionMinimaDelContexto * estimado) - 1;
            return RespuestasProveedor.Texto(peticion, Respuesta, reportado, 5);
        });
        using var fabrica = Fabrica(null, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Single(manejador.Peticiones);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Error, evento.Resultado);
        AfirmacionesDeContexto.AfirmarContextoTruncadoRegistrado(evento);
        Assert.False(AfirmacionesDeReintento.ReintentoRegistrado(evento));
        Assert.Contains(fabrica.Logs.Mensajes, m => m.Contains(estimado.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal) && m.Contains(reportado.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("promedio ponderado", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(ReportesDelProveedor))]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199e2e5_El_limite_el_uso_real_la_falta_de_usage_y_otro_proveedor_no_detectan_truncado(string caso, string? llave, int reporte, bool truncado)
    {
        Assert.NotEmpty(caso);
        var manejador = new ManejadorLlmFalso((peticion, _) =>
        {
            var reportado = Reportado(ConsumoRealistaHttp.Estimados(peticion), reporte);
            return reporte == 2 ? SinUso(Respuesta) : RespuestasProveedor.Texto(peticion, Respuesta, reportado, 5);
        });
        using var fabrica = Fabrica(llave, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Respuesta, respuesta.Respuesta);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Respondida, evento.Resultado);
        Assert.Equal(truncado, AfirmacionesDeContexto.ContextoTruncadoRegistrado(evento));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199e5_Con_openai_un_prompt_tokens_de_10_no_se_trata_como_truncado()
    {
        var manejador = new ManejadorLlmFalso((peticion, _) => RespuestasProveedor.Texto(peticion, Respuesta, 10, 5));
        using var fabrica = Fabrica(LlaveDeOpenAi, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(Respuesta, respuesta.Respuesta);
        Assert.Single(manejador.Peticiones);
        Assert.False(AfirmacionesDeContexto.ContextoTruncadoRegistrado(Assert.Single(fabrica.Auditoria.Eventos)));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-66")]
    public async Task ESC_199e6_Con_ollama_un_prompt_tokens_de_10_se_detecta_como_truncado_sin_minimo_de_verosimilitud()
    {
        var manejador = new ManejadorLlmFalso((peticion, _) => RespuestasProveedor.Texto(peticion, Respuesta, 10, 5));
        using var fabrica = Fabrica(null, manejador);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Single(manejador.Peticiones);
        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Error, evento.Resultado);
        AfirmacionesDeContexto.AfirmarContextoTruncadoRegistrado(evento);
    }

    [Theory]
    [InlineData("estudiante", "EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?")]
    [InlineData("estudiante", "EST-001", Pregunta)]
    [InlineData("estudiante", "EST-001", "¿Cuántas faltas tengo en Bases de Datos?")]
    [InlineData("estudiante", "EST-001", "¿Cómo se calcula la nota definitiva de una asignatura?")]
    [InlineData("estudiante", "EST-004", "¿Qué nota definitiva tengo en Contabilidad?")]
    [InlineData("coordinador", "COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?")]
    [InlineData("estudiante", "EST-001", "¿Cuál es el horario de atención de Bienestar?")]
    [InlineData("estudiante", "EST-004", "¿Qué requisitos tiene la beca por rendimiento académico?")]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199d_Cada_llamada_de_Q10_1_a_8_estima_3000_tokens_o_menos(string rol, string id, string pregunta)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true);

        await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.All(fabrica.Llm.Solicitudes, solicitud => AfirmarTope(solicitud));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199d_Un_turno_de_herramienta_de_notas_de_EST_001_estima_3000_tokens_o_menos()
    {
        var llm = new ClienteLlmDoble((_, numero) => numero == 0
            ? new RespuestaLlm(null, [new LlamadaHerramienta("lectura-notas", HerramientasAsistente.Notas, "{}")], ClienteLlmDoble.UsoPorDefecto, false)
            : new RespuestaLlm("Revisa el detalle.", [], ClienteLlmDoble.UsoPorDefecto, false));
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        await fabrica.PreguntarEstudianteAsync("EST-001", "¿Qué dice el reglamento sobre la habilitación de las asignaturas?");

        Assert.Equal(2, fabrica.Llm.Solicitudes.Count);
        Assert.All(fabrica.Llm.Solicitudes, solicitud => AfirmarTope(solicitud));
    }

    [Fact]
    [Trait("CA", "CA-23")]
    [Trait("ESC", "ESC-199")]
    [Trait("D", "D-57")]
    public async Task ESC_199d_El_reintento_de_Q10_6_con_la_prelectura_estima_3000_tokens_o_menos()
    {
        var llm = GuionesPorTurno.Texto("Mateo Vargas perdió Estadística.", "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).");
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        await fabrica.PreguntarCoordinadorAsync("COORD-02", "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?");

        Assert.Equal(1, GuionesPorTurno.Reintentos(fabrica.Llm));
        Assert.All(fabrica.Llm.Solicitudes, solicitud => AfirmarTope(solicitud));
    }

    private static void AfirmarTope(SolicitudLlm solicitud)
    {
        var caracteres = solicitud.PromptSistema.Length + solicitud.Mensajes.Sum(m => m.Contenido.Length);
        var estimados = (int)Math.Ceiling(caracteres / CaracteresPorToken);
        Assert.True(estimados <= TopeDeTokensEstimados, $"{caracteres} caracteres estiman {estimados} tokens y el tope es {TopeDeTokensEstimados}");
    }
}
