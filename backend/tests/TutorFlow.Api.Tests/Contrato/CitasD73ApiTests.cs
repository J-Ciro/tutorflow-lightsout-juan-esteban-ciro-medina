using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class CitasD73ApiTests
{
    private const string Seccion2 = "evaluacion_y_calificaciones.md · Sección 2";
    private const string Seccion3 = "evaluacion_y_calificaciones.md · Sección 3";
    private const string Seccion4 = "evaluacion_y_calificaciones.md · Sección 4";
    private const string Articulo3 = "reglamento_academico_2025.md · Artículo 3";
    private const string Articulo5 = "reglamento_academico_2025.md · Artículo 5";
    private const string Articulo11 = "reglamento_academico_2025.md · Artículo 11";
    private const string Notas = "Datos del estudiante · Notas";
    private const string Asistencia = "Datos del estudiante · Asistencia";
    private const string ProgramaIngSis = "Datos del programa · ING-SIS";
    private const string ProgramaAdm = "Datos del programa · ADM";
    private const string PreguntaP3 = "¿Cuántas faltas tengo en Bases de Datos?";
    private const string DesgloseDeBasesDeDatos = "Bases de Datos: faltas 2 de 48 clases programadas (faltas restantes: 5; máximo sin perder: 7). Si superas 7 inasistencias pierdes la asignatura por inasistencia.";
    private const string PreguntaP4 = "¿Cómo se calcula la nota definitiva de una asignatura?";
    private const string PreguntaP6 = "¿Qué estudiantes de mi programa tienen alguna asignatura perdida?";
    private const string PreguntaDosMilVeintitres = "¿Qué decía el reglamento de 2023 sobre la habilitación?";
    private const string TextoP4SoloConSeccion3 = "La nota definitiva pondera los tres cortes con sus pesos: 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal (evaluacion_y_calificaciones.md · Sección 3).";
    private const string TextoP4SinCitas = "La nota definitiva pondera los tres cortes con sus pesos: 30% el corte 1, 30% el corte 2 y 40% el corte 3, y se redondea a una cifra decimal.";
    private const string TextoDelReglamentoDerogado = "El reglamento de 2023 fue derogado.";

    public static TheoryData<string, string, string[]> TextosDeP4 => new()
    {
        { "solo la Sección 3", TextoP4SoloConSeccion3, [Seccion3, Seccion2] },
        { "ninguna cita", TextoP4SinCitas, [Seccion2, Seccion3] }
    };

    public static TheoryData<string, string> TextosDeP3 => new()
    {
        { "sin citas", "Bases de Datos: 2 faltas de 48 clases." },
        { "con las reglas aplicadas copiadas", $"Bases de Datos: 2 faltas de 48 clases ({Seccion3}; {Seccion4})." },
        { "con la cita al Artículo 11", $"Bases de Datos: 2 faltas de 48 clases ({Articulo11})." }
    };

    [Theory]
    [MemberData(nameof(TextosDeP3))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-003")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_003_Las_citas_de_documento_de_las_faltas_son_exactamente_el_Articulo_11(string caso, string texto)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaP3);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal([Articulo11, Notas, Asistencia], respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Equal(ResultadoAuditoria.Respondida, Assert.Single(fabrica.Auditoria.Eventos).Resultado);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177j_Las_citas_copiadas_a_las_secciones_3_y_4_salen_del_texto_y_de_las_citas()
    {
        var texto = $"Bases de Datos: 2 faltas de 48 clases ({Seccion3}; {Seccion4}).";
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaP3);

        Assert.Contains(DesgloseDeBasesDeDatos, respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Total: 2 faltas", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("evaluacion_y_calificaciones.md", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Equal([Articulo11], respuesta.Citas.Where(c => c.StartsWith("reglamento", StringComparison.Ordinal) || c.StartsWith("evaluacion", StringComparison.Ordinal)));
        Assert.Equal(1, fabrica.Llm.Llamadas);
        Assert.DoesNotContain(Seccion3, respuesta.Citas);
        Assert.DoesNotContain(Seccion4, respuesta.Citas);
    }

    [Theory]
    [MemberData(nameof(TextosDeP4))]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-004")]
    [Trait("D", "D-73")]
    public async Task ESC_004_Las_citas_incluyen_las_secciones_2_y_3_aunque_el_modelo_escriba_solo_la_3_o_ninguna(string caso, string texto, string[] documentos)
    {
        Assert.NotEmpty(caso);
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaP4);

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(documentos, respuesta.Citas);
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain("\\", respuesta.Respuesta, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-005")]
    [Trait("D", "D-73")]
    public async Task ESC_005_Con_solo_la_cifra_el_documento_queda_vacio_y_los_datos_son_notas_y_asistencia()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "3.9."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", "¿Qué nota definitiva tengo en Contabilidad?");

        Assert.Equal("Datos de Camilo Rojas (EST-004)\nDefinitivas: Contabilidad 3.9", respuesta.Respuesta.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Contains(Notas, respuesta.Citas);
        Assert.Equal([Seccion3], respuesta.Citas.Where(c => !c.StartsWith("Datos del", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("CA", "CA-01")]
    [Trait("ESC", "ESC-006")]
    [Trait("D", "D-73")]
    public async Task ESC_006_Valentina_Castro_llega_con_el_Articulo_5_y_los_datos_del_programa_ADM()
    {
        const string texto = "Valentina Castro: Microeconomía (1.8) y Estadística (2.3).";
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.ConsultaDatos(redactar: _ => texto), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-02", PreguntaP6);

        Assert.Equal("Estudiantes con asignaturas perdidas: Valentina Castro (Microeconomía 1.8; Estadística 2.3)", respuesta.Respuesta.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Equal([Articulo5, ProgramaAdm], respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-22")]
    [Trait("ESC", "ESC-177")]
    [Trait("D", "D-73")]
    public async Task ESC_177k_El_conteo_del_programa_no_recibe_reglas_aplicadas_ni_respaldo()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Tu programa tiene 3 estudiantes."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarCoordinadorAsync("COORD-01", "¿Cuántos estudiantes tiene mi programa?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal("Tu programa tiene 3 estudiantes.", respuesta.Respuesta);
        Assert.Equal([ProgramaIngSis], respuesta.Citas);
        Assert.True(respuesta.UsoDatosEstudiante);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-014")]
    [Trait("D", "D-73")]
    public async Task ESC_014_Con_2023_en_la_pregunta_el_Articulo_3_se_entrega_y_se_cita_y_el_prompt_no_menciona_el_2023()
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => TextoDelReglamentoDerogado), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", PreguntaDosMilVeintitres);

        var solicitud = Assert.Single(fabrica.Llm.Solicitudes);
        Assert.Contains(GuionesLlm.Bloques(solicitud), b => b.Fuente == Articulo3);
        Assert.DoesNotContain("2023", solicitud.PromptSistema, StringComparison.Ordinal);
        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(TextoDelReglamentoDerogado, respuesta.Respuesta);
        Assert.Contains(Articulo3, respuesta.Citas);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("reglamento_academico_2023.md", StringComparison.Ordinal));
        Assert.False(respuesta.UsoDatosEstudiante);
    }

    [Theory]
    [InlineData("estudiante", "EST-001")]
    [InlineData("coordinador", "COORD-02")]
    [Trait("CA", "CA-20")]
    [Trait("ESC", "ESC-191")]
    [Trait("D", "D-73")]
    public async Task ESC_191_El_prompt_enviado_pide_texto_plano_sin_latex_y_no_menciona_el_2023(string rol, string id)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Revisa el detalle."), conocimientoReal: true);

        await fabrica.PreguntarAsync(rol, id, "¿Cuál es el horario de atención de Bienestar?");

        var instrucciones = ConstructorPrompts.Instrucciones(Assert.Single(fabrica.Llm.Solicitudes).PromptSistema);
        Assert.Contains("LaTeX", instrucciones, StringComparison.Ordinal);
        Assert.Contains("fórmulas", instrucciones, StringComparison.Ordinal);
        Assert.DoesNotContain("2023", instrucciones, StringComparison.Ordinal);
        Assert.DoesNotContain("derog", instrucciones, StringComparison.OrdinalIgnoreCase);
    }
}
