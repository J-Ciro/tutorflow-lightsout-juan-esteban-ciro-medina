using TutorFlow.Api.Tests.Dobles;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitaDelArticulo5D4ApiTests
{
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Articulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";

    [Theory]
    [InlineData("Tu nota definitiva en Contabilidad es 3.9 (reglamento_academico_2025.md · Artículo 5).")]
    [InlineData("Tu nota definitiva en Contabilidad es 3.9 (evaluacion_y_calificaciones.md · Sección 3; reglamento_academico_2025.md · Artículo 5).")]
    [InlineData("Contabilidad: 3.9. Se aprueba con 3.0 (reglamento_academico_2025.md · Artículo 5).")]
    [Trait("Hallazgo", "D4")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-005")]
    public async Task D99_D4_P5_con_el_Articulo_5_escrito_cita_solo_la_Seccion_3_como_documento(string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", "¿Qué nota definitiva tengo en Contabilidad?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal([Seccion3, Notas, Asistencia], respuesta.Citas);
        Assert.DoesNotContain(Articulo5, respuesta.Citas);
    }

    [Fact]
    [Trait("Hallazgo", "D4")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-177")]
    public async Task D99_D4_un_estudiante_con_una_asignatura_perdida_que_escribe_perdiste_conserva_el_Articulo_5()
    {
        const string texto = "Perdiste Microeconomía (1.8) y Estadística (2.3).";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-005", "¿Qué dice el reglamento sobre la habilitación?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(Articulo5, respuesta.Citas);
    }
}
