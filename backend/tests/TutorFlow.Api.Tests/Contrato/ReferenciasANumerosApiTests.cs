using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ReferenciasANumerosApiTests
{
    private const string PreguntaPromedio = "¿Cuál es mi promedio?";
    private const string PreguntaPromedioPonderado = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaNotaMinima = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaBeca = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string CitaSeccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string CitaBeca = "bienestar_y_becas.md · Sección 3.1";

    public static TheoryData<string> TextosDeBecaConLaReferencia => new()
    {
        "Se aprueba con un promedio ponderado del periodo igual o superior a 4.3, sin asignaturas perdidas, y matriculando al menos 12 créditos (bienestar_y_becas.md · Sección 3.1).",
        "Para obtener la beca por rendimiento académico, se deben cumplir los siguientes requisitos: promedio de 4.3 o más, sin asignaturas perdidas y 12 créditos. Estos requisitos se respaldan en bienestar_y_becas.md · Sección 3.1.",
        "La beca por rendimiento exige 4.3 de promedio ponderado (Sección 3.1) y matricular 12 créditos (bienestar_y_becas.md · Sección 3.1)."
    };

    public static TheoryData<string, string> TextosConCifraNoRespaldada => new()
    {
        { PreguntaPromedio, "Tu promedio es 4.40 (Sección 4)." },
        { PreguntaNotaMinima, "Una asignatura se aprueba con 3.7 según el Artículo 5." },
        { PreguntaBeca, "La beca exige 4.7 de promedio (bienestar_y_becas.md · Sección 3.1)." }
    };

    [Theory]
    [MemberData(nameof(TextosDeBecaConLaReferencia))]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-174")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_174a_El_3_1_de_la_referencia_a_seccion_no_se_verifica_como_cifra(string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaBeca);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(texto, respuesta.Respuesta);
        Assert.Contains(CitaBeca, respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(TextosConCifraNoRespaldada))]
    [Trait("CA", "CA-05")]
    [Trait("ESC", "ESC-174")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_174_Una_cifra_no_respaldada_junto_a_una_referencia_se_sigue_verificando(string pregunta, string texto)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(Textos.NoVerificable, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(ResultadoAuditoria.NumeroNoVerificado, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-174")]
    [Trait("RF", "SEG-10c")]
    public async Task ESC_174f_Las_secciones_3_y_4_de_una_referencia_en_lista_no_bloquean_el_promedio_y_la_referencia_sale_entera()
    {
        const string texto = "Tu promedio es 4.39 (evaluacion_y_calificaciones.md · Secciones 3 y 4).";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaPromedioPonderado);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("Tu promedio es 4.39.", respuesta.Respuesta);
        Assert.Equal([CitaSeccion4, "Datos del estudiante · Notas", "Datos del estudiante · Asistencia"], respuesta.Citas);
        Assert.DoesNotContain("Secciones", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("Sección 3", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
