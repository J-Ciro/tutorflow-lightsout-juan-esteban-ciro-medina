using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class FormasVerbalesD74ApiTests
{
    private const string Articulo6 = "reglamento_academico_2025.md · Artículo 6";
    private const string Articulo7 = "reglamento_academico_2025.md · Artículo 7";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string PreguntaSinTerminoDeFaltas = "¿Qué dice el reglamento sobre la habilitación?";
    private const string PreguntaHabilitar = "¿Qué dice el reglamento sobre la habilitación?";

    public static TheoryData<string, string> FormasDeInasistencia => new()
    {
        { "faltar", "Puedes faltar 5 clases más en Bases de Datos." },
        { "faltaste", "Faltaste a 2 clases de Bases de Datos." },
        { "faltado", "Has faltado a 2 clases de Bases de Datos." }
    };

    public static TheoryData<string, string> FormasDeHabilitacion => new()
    {
        { "no puedes habilitar", "No puedes habilitar Bases de Datos." },
        { "habilitarla", "Bases de Datos no puedes habilitarla." },
        { "no se puede habilitar", "Bases de Datos no se puede habilitar." }
    };

    [Theory]
    [MemberData(nameof(FormasDeInasistencia))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-027")]
    [Trait("D", "D-74")]
    public async Task ESC_027_Las_formas_verbales_de_inasistencia_citan_el_Articulo_11(string forma, string texto)
    {
        Assert.NotEmpty(forma);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaSinTerminoDeFaltas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(Articulo11, respuesta.Citas);
    }

    [Theory]
    [MemberData(nameof(FormasDeHabilitacion))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-032")]
    [Trait("D", "D-74")]
    public async Task ESC_032_Las_formas_verbales_de_no_habilitar_citan_los_Articulos_7_y_6(string forma, string texto)
    {
        Assert.NotEmpty(forma);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-002", PreguntaHabilitar);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(Articulo7, respuesta.Citas);
        Assert.Contains(Articulo6, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-02")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-74")]
    public async Task Le_falta_calificar_el_corte_3_no_cita_el_Articulo_11_aunque_este_en_las_reglas_aplicadas()
    {
        const string texto = "A Bases de Datos le falta calificar el corte 3.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaSinTerminoDeFaltas);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.DoesNotContain(Articulo11, respuesta.Citas);
        var reglas = GuionesLlm.Bloques(fabrica.Llm.Solicitudes[^1]).SelectMany(b => b.Cuerpo.Split((char)10)).Where(l => l.StartsWith("Reglas aplicadas", StringComparison.Ordinal));
        Assert.Contains(reglas, l => l.Contains(Articulo11, StringComparison.Ordinal));
    }
}
