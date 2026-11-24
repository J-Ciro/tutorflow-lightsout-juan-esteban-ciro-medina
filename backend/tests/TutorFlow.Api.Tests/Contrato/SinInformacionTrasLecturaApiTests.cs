using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class SinInformacionTrasLecturaApiTests
{
    private const string PreguntaPerdidas = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("D", "D-66")]
    public async Task COMP_03_tras_el_resumen_propio_responde_citas_vacias_y_uso_true()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => Textos.SinInformacion), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo se calcula el promedio ponderado del periodo?");

        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("D", "D-66")]
    public async Task COMP_03_tras_leer_notas_con_la_herramienta_responde_citas_vacias_y_uso_true()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => Textos.SinInformacion), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo se calcula el promedio ponderado del periodo?");

        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("D", "D-66")]
    public async Task COMP_03_tras_la_prelectura_del_programa_responde_citas_vacias_y_uso_true()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => Textos.SinInformacion), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", "¿Qué dice el reglamento sobre la habilitación?");

        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-156")]
    [Trait("D", "D-66")]
    public async Task COMP_03_sin_datos_leidos_responde_citas_vacias_y_uso_false()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => Textos.SinInformacion), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuál es la nota mínima para aprobar una asignatura?");

        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
    }
}
