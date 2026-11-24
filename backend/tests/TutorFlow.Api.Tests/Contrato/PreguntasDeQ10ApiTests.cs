using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class PreguntasDeQ10ApiTests
{
    private const string Seccion2 = "evaluacion_y_calificaciones.md · Sección 2";
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Beca31 = "bienestar_y_becas.md · Sección 3.1";
    private const string Beca32 = "bienestar_y_becas.md · Sección 3.2";
    private const string Bienestar1 = "bienestar_y_becas.md · Sección 1";
    private const string Articulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Articulo6 = "reglamento_academico_2025.md · Artículo 6";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Articulo20 = "reglamento_academico_2025.md · Artículo 20";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";
    private const string ProgramaAdm = "Datos del programa · ADM";
    private const string PuntajeRelativo = "Conocimiento:PuntajeRelativoMinimo";
    private const string PreguntaP1 = "¿Cuál es la nota mínima para aprobar una asignatura?";
    private const string PreguntaP2 = "¿Cuál es mi promedio ponderado del periodo?";
    private const string PreguntaP3 = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string PreguntaP4 = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaP5 = "¿Qué nota definitiva tengo en Contabilidad?";
    private const string PreguntaP6 = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaP7 = "¿Cuál es el horario de atención de Bienestar?";
    private const string PreguntaP8 = "¿Qué requisitos tiene la beca por rendimiento académico?";
    private const string Definitivas = "Definitivas: Cálculo Integral 4.5, Programación Orientada a Objetos 4.8, Bases de Datos 4.1, Inglés III 4.0.";
    private const string TextoP8 = "Requisitos: promedio de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos; da 50% de descuento en la matrícula siguiente.";

    public static TheoryData<string, string, string, string[]> SeccionesEntregadas => new()
    {
        { "Q10 #8", "EST-004", PreguntaP8, [Beca31] },
        { "Q10 #4", "EST-001", PreguntaP4, [Seccion3, Seccion2, Articulo5] },
        { "Q10 #7", "EST-001", PreguntaP7, [Bienestar1] },
        { "Q10 #1", "EST-001", PreguntaP1, [Articulo5, Seccion3] },
        { "Q10 #5", "EST-004", PreguntaP5, [Seccion3, Seccion2, Articulo5] }
    };

    public static TheoryData<string, string, string, string, string[], bool> CitasPorPregunta => new()
    {
        { "Q10 #1 con cita escrita", "EST-001", PreguntaP1, "Se aprueba una asignatura con una nota definitiva de 3.0 o más, en una escala de 0.0 a 5.0 (reglamento_academico_2025.md · Artículo 5).", [Articulo5], false },
        { "Q10 #1 sin cita escrita", "EST-001", PreguntaP1, "Una asignatura se aprueba con 3.0.", [Articulo5], false },
        { "Q10 #2", "EST-001", PreguntaP2, $"Tu promedio es 4.39. {Definitivas}", [Seccion4, Seccion3, Notas, Asistencia], true },
        { "Q10 #2 literal", "EST-001", PreguntaP2, $"4.39. {Definitivas}", [Seccion4, Seccion3, Notas, Asistencia], true },
        { "Q10 #3", "EST-001", PreguntaP3, "Bases de Datos: 2 faltas de 48 clases.", [Articulo11, Notas, Asistencia], true },
        { "Q10 #5", "EST-004", PreguntaP5, "Contabilidad: 3.9. Se aprueba con una definitiva de 3.0 o más (reglamento_academico_2025.md · Artículo 5).", [Seccion3, Notas, Asistencia], true },
        { "Q10 #7", "EST-001", PreguntaP7, "Lunes a viernes de 8:00 a 17:00 y sábados de 8:00 a 12:00, en el edificio B, piso 2.", [Bienestar1], false },
        { "Q10 #8 con el Artículo 20 escrito", "EST-004", PreguntaP8, $"{TextoP8.TrimEnd('.')} ({Articulo20}).", [Beca31], false },
        { "Q10 #8 sin cita escrita", "EST-004", PreguntaP8, TextoP8, [Beca31], false },
        { "Q10 #8 literal", "EST-004", PreguntaP8, "Promedio ponderado del periodo de 4.3 o más, ninguna asignatura perdida y al menos 12 créditos matriculados. Da 50% de descuento en la matrícula siguiente.", [Beca31], false }
    };

    private static Dictionary<string, string?> ConUmbralesPorDefecto() => [];

    private static IReadOnlyList<string> FuentesDeDocumentos(SolicitudLlm solicitud) =>
        [.. GuionesLlm.Bloques(solicitud)
            .Select(b => b.Fuente)
            .Where(f => !f.StartsWith("Datos", StringComparison.Ordinal) && !f.StartsWith("Resumen", StringComparison.Ordinal))];

    [Theory]
    [MemberData(nameof(SeccionesEntregadas))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-54")]
    public async Task ESC_194d_Con_el_indice_real_y_los_umbrales_55_y_40_se_entregan_los_conjuntos_de_ESC_198a(string caso, string usuario, string pregunta, string[] esperadas)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.Equal(esperadas, FuentesDeDocumentos(Assert.Single(fabrica.Llm.Solicitudes)));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-54")]
    public async Task ESC_194d_Q10_8_no_entrega_la_Seccion_3_2_ni_la_Seccion_3_de_evaluacion()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaP8);

        var entregadas = FuentesDeDocumentos(Assert.Single(fabrica.Llm.Solicitudes));
        Assert.DoesNotContain(Beca32, entregadas);
        Assert.DoesNotContain(Seccion3, entregadas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-54")]
    public async Task ESC_194d_Con_un_puntaje_relativo_de_1_el_umbral_deja_pasar_las_secciones_recuperadas_de_P8()
    {
        var configuracion = new Dictionary<string, string?> { [PuntajeRelativo] = "1" };
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true, configuracion: configuracion);

        await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaP8);

        var entregadas = FuentesDeDocumentos(Assert.Single(fabrica.Llm.Solicitudes));
        Assert.Contains(Beca31, entregadas);
        Assert.Contains(entregadas, e => e.StartsWith("reglamento_academico_2025.md", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(CitasPorPregunta))]
    [Trait("CA", "CA-01")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task Las_citas_de_documento_de_cada_pregunta_de_Q10_son_exactamente_las_de_la_spec(string caso, string usuario, string pregunta, string texto, string[] citas, bool usaDatos)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        var respuesta = await fabrica.PreguntarEstudianteAsync(usuario, pregunta);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(citas, respuesta.Citas);
        Assert.Equal(usaDatos, respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task P6_con_la_herramienta_de_programa_cita_solo_el_Articulo_5_y_los_datos_del_programa()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaP6);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal([Articulo5, ProgramaAdm], respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-53")]
    public async Task ESC_194g_Nombrar_la_beca_en_la_respuesta_del_coordinador_no_agrega_citas_de_bienestar()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3); para la beca revisa con Bienestar.";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaP6);

        Assert.StartsWith("Estudiantes con asignaturas perdidas: ", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("Bienestar", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal([Articulo5, ProgramaAdm], respuesta.Citas);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("bienestar_y_becas.md", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-194")]
    [Trait("D", "D-54")]
    [Trait("D", "D-63")]
    public async Task ESC_194e_El_Articulo_20_escrito_por_el_modelo_para_Q10_8_se_elimina_del_texto_y_no_se_cita()
    {
        var texto = $"{TextoP8.TrimEnd('.')} ({Articulo20}).";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true, configuracion: ConUmbralesPorDefecto());

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", PreguntaP8);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(TextoP8, respuesta.Respuesta);
        Assert.Equal([Beca31], respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }
}
