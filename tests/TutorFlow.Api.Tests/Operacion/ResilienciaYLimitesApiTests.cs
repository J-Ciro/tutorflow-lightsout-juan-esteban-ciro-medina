using System.Diagnostics;
using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Operacion;

public sealed class ResilienciaApiTests
{
    private const string Pregunta = "¿Cuál es mi promedio?";

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-108")]
    public async Task ESC_108_Api_academica_caida_responde_503_generico_sin_detalles_internos()
    {
        using var fabrica = new FabricaApi(api: ApiAcademicaDoble.Caida());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.DoesNotContain("api academica caida", respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-108")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_108_Api_academica_que_no_responde_produce_503_en_menos_de_7_segundos()
    {
        using var fabrica = new FabricaApi(api: ApiAcademicaDoble.QueNuncaResponde());
        var reloj = Stopwatch.StartNew();

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        reloj.Stop();
        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.True(reloj.Elapsed < TimeSpan.FromSeconds(7), $"tardó {reloj.Elapsed}");
    }

    [Theory]
    [InlineData("http")]
    [InlineData("cancelado")]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-109")]
    public async Task ESC_109_Llm_caido_o_con_timeout_responde_503_sin_url_modelo_ni_llave(string falla)
    {
        Exception excepcion = falla == "http"
            ? new HttpRequestException("fallo en https://api.anthropic.com con sk-ant-SECRETO modelo claude-haiku-5-5")
            : new TaskCanceledException("timeout https://api.anthropic.com sk-ant-SECRETO");
        var configuracion = new Dictionary<string, string?> { ["LLM_API_KEY"] = "sk-ant-SECRETO" };
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(excepcion), configuracion: configuracion);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");

        Afirmaciones.ProblemDetails(respuesta, 503);
        Assert.DoesNotContain("anthropic", respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRETO", respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("haiku", respuesta.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fabrica.Logs.Mensajes, m => m.Contains("SECRETO", StringComparison.Ordinal));
        Assert.Equal(ResultadoAuditoria.Error, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-110")]
    public async Task ESC_110_Sin_llm_las_preguntas_denegadas_siguen_dando_403_y_las_normativas_503()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new HttpRequestException("ollama no responde")));

        var denegada = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuáles son las notas de EST-002?");
        var normativa = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");
        var salud = await fabrica.EnviarAsync(HttpMethod.Get, "/api/salud", null);

        Afirmaciones.Negativa(denegada, "estudiante");
        Afirmaciones.ProblemDetails(normativa, 503);
        Assert.Equal(200, salud.Estado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-112")]
    public async Task ESC_112_Un_error_no_controlado_responde_500_con_trace_id_y_sin_stack_trace()
    {
        using var fabrica = new FabricaApi(conocimiento: new ConocimientoQueExplota());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar?");

        Afirmaciones.ProblemDetails(respuesta, 500);
        Assert.False(string.IsNullOrWhiteSpace(respuesta.Json.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("secreto-interno", respuesta.Texto, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", respuesta.Texto, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-13")]
    public async Task Al_superar_el_presupuesto_diario_responde_503_y_al_dia_siguiente_vuelve_a_responder()
    {
        var configuracion = FabricaApi.Precios("1.0", "5.0");
        configuracion["Limites:PresupuestoDiarioUsd"] = "0.001";
        var llm = ClienteLlmDoble.Texto("Respuesta de prueba.", new UsoTokens(1000, 200));
        using var fabrica = new FabricaApi(llm: llm, configuracion: configuracion);

        var primera = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        var segunda = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);
        fabrica.Reloj.Avanzar(TimeSpan.FromDays(1));
        var alDiaSiguiente = await fabrica.PreguntarEstudianteAsync("EST-001", Pregunta);

        Assert.Equal(200, primera.Estado);
        Afirmaciones.ProblemDetails(segunda, 503);
        Assert.Equal(200, alDiaSiguiente.Estado);
    }

    private sealed class ConocimientoQueExplota : IBaseConocimiento
    {
        public IReadOnlyList<TutorFlow.Core.Conocimiento.SeccionDocumento> BuscarSecciones(string consulta, int maximo) =>
            throw new InvalidOperationException("secreto-interno C:\\ruta\\interna");

        public bool ExisteCita(TutorFlow.Core.Conocimiento.Cita cita) => false;
    }
}

public sealed class LimitesApiTests
{
    private const string Pregunta = "¿Cuál es la nota mínima para aprobar?";

    [Fact]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-084")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_084_La_pregunta_61_desde_la_misma_ip_recibe_429_con_retry_after_aunque_cambie_x_forwarded_for()
    {
        using var fabrica = new FabricaApi();
        var ultima = default(RespuestaApi);

        for (var i = 1; i <= 61; i++)
        {
            var encabezados = new Dictionary<string, string> { ["X-Forwarded-For"] = $"203.0.113.{i}" };
            ultima = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, "10.0.0.5", encabezados);
            Assert.Equal(i == 61 ? 429 : 200, ultima.Estado);
        }

        Afirmaciones.ProblemDetails(ultima!, 429);
        Assert.NotNull(ultima!.Encabezado("Retry-After"));
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("ESC", "ESC-142")]
    [Trait("SEG", "SEG-13")]
    public async Task ESC_142_El_limite_es_por_ip_y_no_bloquea_a_la_victima()
    {
        using var fabrica = new FabricaApi();
        RespuestaApi ipA = null!;

        for (var i = 0; i < 61; i++)
        {
            ipA = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, "10.0.0.5");
        }

        var ipB = await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, "10.0.0.9");

        Assert.Equal(429, ipA.Estado);
        Assert.Equal(200, ipB.Estado);
    }

    [Fact]
    [Trait("CA", "CA-13")]
    [Trait("SEG", "SEG-13")]
    public async Task El_limite_se_aplica_antes_de_validar_la_identidad()
    {
        using var fabrica = new FabricaApi();
        RespuestaApi ultima = null!;

        for (var i = 0; i < 61; i++)
        {
            ultima = await fabrica.PreguntarAsync("estudiante", "EST-999", Pregunta, "10.0.0.7");
        }

        Assert.Equal(429, ultima.Estado);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("SEG", "SEG-13")]
    public async Task Cada_respuesta_429_emite_un_evento_de_auditoria()
    {
        using var fabrica = new FabricaApi();

        for (var i = 0; i < 61; i++)
        {
            await fabrica.PreguntarAsync("estudiante", "EST-001", Pregunta, "10.0.0.5");
        }

        Assert.Equal(61, fabrica.Auditoria.Eventos.Count);
    }
}
