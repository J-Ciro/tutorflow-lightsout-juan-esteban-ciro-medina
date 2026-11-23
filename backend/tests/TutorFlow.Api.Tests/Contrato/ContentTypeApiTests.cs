using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ContentTypeApiTests
{
    private const string CuerpoValido = """{"pregunta":"¿Cuál es mi promedio?","usuario":{"rol":"estudiante","id":"EST-001"}}""";
    private const string PreguntaNoEvaluada = "[NO_EVALUADA]";

    public static TheoryData<string?> TiposNoJson => new()
    {
        "text/plain",
        "application/xml",
        "application/x-www-form-urlencoded",
        "text/json",
        "application/vnd.api+json",
        null
    };

    public static TheoryData<string> TiposJson => new()
    {
        "application/json",
        "application/json; charset=utf-8",
        "APPLICATION/JSON; charset=UTF-8"
    };

    [Theory]
    [MemberData(nameof(TiposNoJson))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-150")]
    public async Task ESC_150_Un_content_type_distinto_de_json_responde_415_sin_llamar_al_llm_ni_a_la_api(string? tipo)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarConTipoAsync(CuerpoValido, tipo);

        Afirmaciones.ProblemDetails(respuesta, 415);
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Empty(fabrica.Api.Llamadas);
    }

    [Theory]
    [MemberData(nameof(TiposNoJson))]
    [Trait("CA", "CA-08")]
    [Trait("ESC", "ESC-150")]
    public async Task ESC_150_El_415_se_audita_como_invalida_con_la_pregunta_no_evaluada(string? tipo)
    {
        using var fabrica = new FabricaApi();

        await fabrica.EnviarConTipoAsync(CuerpoValido, tipo);

        var evento = Assert.Single(fabrica.Auditoria.Eventos);
        Assert.Equal(ResultadoAuditoria.Invalida, evento.Resultado);
        Assert.Equal(PreguntaNoEvaluada, evento.Pregunta);
    }

    [Theory]
    [MemberData(nameof(TiposJson))]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-150")]
    public async Task ESC_150_Con_application_json_con_o_sin_charset_y_sin_distinguir_mayusculas_responde_200(string tipo)
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarConTipoAsync(CuerpoValido, tipo);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1, fabrica.Llm.Llamadas);
    }

    [Fact]
    [Trait("CA", "CA-07")]
    [Trait("ESC", "ESC-150")]
    public async Task ESC_150_El_415_no_expone_detalles_internos()
    {
        using var fabrica = new FabricaApi();

        var respuesta = await fabrica.EnviarConTipoAsync(CuerpoValido, "text/plain");

        Afirmaciones.ProblemDetails(respuesta, 415);
        Afirmaciones.SinDetallesInternos(respuesta);
        Assert.DoesNotContain("text/plain", respuesta.Texto, StringComparison.OrdinalIgnoreCase);
    }
}
