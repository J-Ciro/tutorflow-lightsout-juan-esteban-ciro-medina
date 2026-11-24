using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitasTrasLaLimpiezaApiTests
{
    private const string PreguntaPromedio = "¿Cómo se calcula el promedio ponderado del periodo?";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";

    public static TheoryData<string, string, string> TextosConReferencias => new()
    {
        { "ESC-183e", "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4).", "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Sección 4)." },
        { "ESC-183f", "Tu promedio es 4.39 (x.md · Sección 9).", "Tu promedio es 4.39." }
    };

    [Theory]
    [MemberData(nameof(TextosConReferencias))]
    [Trait("CA", "CA-16")]
    [Trait("ESC", "ESC-183")]
    [Trait("D", "D-53")]
    public async Task ESC_183_La_respuesta_cita_la_Seccion_4_por_el_campo_promedio_y_los_datos_leidos(string caso, string texto, string esperada)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.LeeNotasYAsistencia(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedio);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(esperada, respuesta.Respuesta);
        Assert.Equal([Seccion4, Notas, Asistencia], respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
