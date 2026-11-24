using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitasPorTemaYValorApiTests
{
    private const string Seccion2 = "evaluacion_y_calificaciones.md · Sección 2";
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Beca31 = "bienestar_y_becas.md · Sección 3.1";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";
    private const string PreguntaP2 = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaP4 = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaP5 = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaP7 = "¿Cuál es el horario de atención de Bienestar?";
    private const string PreguntaP8 = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string TextoRealDeP4 = "La nota definitiva de una asignatura se calcula como el promedio ponderado de los tres cortes, con los siguientes pesos: definitiva = 0.3 × corte 1 + 0.3 × corte 2 + 0.4 × corte 3 Esta nota se redondea a una cifra decimal: si la segunda cifra decimal es 5 o más, se redondea hacia arriba. Mientras un corte no esté calificado, la asignatura no tendrá una nota definitiva (evaluacion_y_calificaciones.md · Sección 3).";
    private const string TextoDeP8 = "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente.";
    private const string Definitivas = "Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0.";

    public static TheoryData<string, string, string, string, string, string[], bool> RespuestasRespondidas => new()
    {
        { "b", "EST-001", PreguntaP4, TextoRealDeP4, TextoRealDeP4, [Seccion3, Seccion2], false },
        { "c", "EST-001", PreguntaP4, "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.", "La definitiva pondera 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.", [Seccion2, Seccion3], false },
        { "d1", "EST-004", PreguntaP5, "Contabilidad: 3.9. La definitiva se calcula con los pesos.", "Contabilidad: 3.9. La definitiva se calcula con los pesos.", [Seccion3, Notas, Asistencia], true },
        { "d2", "EST-004", PreguntaP8, TextoDeP8, TextoDeP8, [Beca31], false },
        { "d3", "EST-001", PreguntaP2, "Tu promedio es 4.39 y se calcula con los pesos y se redondea.", "Tu promedio es 4.39 y se calcula con los pesos y se redondea.", [Seccion4, Notas, Asistencia], true },
        { "e", "EST-001", PreguntaP2, $"Tu promedio es 4.39. {Definitivas}", $"Tu promedio es 4.39. {Definitivas}", [Seccion4, Seccion3, Notas, Asistencia], true },
        { "f1", "EST-004", PreguntaP5, "Contabilidad: 3.9.", "Contabilidad: 3.9.", [Seccion3, Notas, Asistencia], true },
        { "f2", "EST-004", PreguntaP5, "3.9.", "3.9.", [Notas, Asistencia], true },
        { "k1", "EST-004", PreguntaP5, "Tu nota definitiva en Contabilidad es 3.9 (documento_ejemplo.md · Sección 2).", "Tu nota definitiva en Contabilidad es 3.9.", [Seccion3, Notas, Asistencia], true },
        { "l1", "EST-001", PreguntaP2, "Según el reglamento evaluación y calificaciones.md · Sección 4 tu promedio es 4.39.", "Tu promedio es 4.39.", [Seccion4, Notas, Asistencia], true },
        { "l2", "EST-001", PreguntaP2, "Tu promedio es 4.39. Esta información proviene del documento de evaluación.", "Tu promedio es 4.39.", [Seccion4, Notas, Asistencia], true }
    };

    public static TheoryData<string, string, string, string, bool> RespuestasSinInformacion => new()
    {
        { "f3", "EST-004", PreguntaP5, "11.", true },
        { "k2", "EST-001", PreguntaP7, "(documento_ejemplo.md · Sección 1)", false }
    };

    [Theory]
    [MemberData(nameof(RespuestasRespondidas))]
    [Trait("CA", "CA-22")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    [Trait("D", "D-56")]
    [Trait("D", "D-60")]
    public async Task ESC_198_El_texto_verificado_y_sus_citas_son_exactamente_los_de_la_tabla(string caso, string usuario, string pregunta, string texto, string esperado, string[] citas, bool usoDatos)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.Equal(esperado, respuesta.Respuesta);
        Assert.Equal(citas, respuesta.Citas);
        Assert.Equal(usoDatos, respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Theory]
    [MemberData(nameof(RespuestasSinInformacion))]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-58")]
    [Trait("D", "D-63")]
    public async Task ESC_198_Un_texto_que_queda_sin_contenido_responde_COMP_03_con_el_uso_de_datos_leido(string caso, string usuario, string pregunta, string texto, bool usoDatos)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.Equal(Textos.SinInformacion, respuesta.Respuesta);
        Assert.Empty(respuesta.Citas);
        Assert.Equal(usoDatos, respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.SinInformacion, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-198")]
    [Trait("D", "D-55")]
    public async Task ESC_198b_El_Articulo_5_entregado_no_se_cita_cuando_ya_hay_citas_de_documento()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => TextoRealDeP4), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaP4);

        Assert.DoesNotContain("reglamento_academico_2025.md · Artículo 5", respuesta.Citas);
    }
}
