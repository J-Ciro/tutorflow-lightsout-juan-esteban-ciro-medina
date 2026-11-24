using TutorFlow.Api.Tests.Dobles;
using TutorFlow.Core.Asistente;
using TutorFlow.Pruebas.Soporte;

namespace TutorFlow.Api.Tests.Contrato;

public sealed class ResumenPorAsignaturaApiTests
{
    private const string RespuestaGenerica = "Revisa el detalle de tus asignaturas.";
    private const string CitaNotas = "Datos del estudiante · Notas";
    private const string CitaAsistencia = "Datos del estudiante · Asistencia";

    public static TheoryData<string, string, string, string> ActivadoresDelResumen => new()
    {
        { "EST-002", "¿Puedo habilitar Bases de Datos?", "Bases de Datos", "ESC-172a" },
        { "EST-001", "¿Cómo voy en Bases de Datos?", "Bases de Datos", "ESC-172b" },
        { "EST-001", "¿Cómo voy en BD-203?", "Bases de Datos", "ESC-172c" },
        { "EST-005", "¿Qué materias puedo habilitar?", "Microeconomía", "ESC-172d" }
    };

    private static ClienteLlmDoble LlmGenerico() => GuionesLlm.SinHerramientas(_ => RespuestaGenerica);

    private static string TextoEnviado(SolicitudLlm solicitud) =>
        solicitud.PromptSistema + "\n" + string.Join("\n", solicitud.Mensajes.Select(m => m.Contenido));

    private static IEnumerable<string> PersonasAjenasA(string estudianteId)
    {
        var datos = DatosReferencia.Cargar();
        return datos.Estudiantes.Where(e => e.Id != estudianteId).SelectMany(e => new[] { e.Nombre, e.Id });
    }

    private static int LecturasDeNotas(FabricaApi fabrica) => fabrica.Api.Llamadas.Count(l => l.StartsWith("ObtenerNotas", StringComparison.Ordinal));

    private static int LecturasDeAsistencia(FabricaApi fabrica) => fabrica.Api.Llamadas.Count(l => l.StartsWith("ObtenerAsistencia", StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(ActivadoresDelResumen))]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-172")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_172_Los_activadores_de_puedo_y_de_asignatura_matriculada_entregan_el_resumen_propio(string id, string pregunta, string asignatura, string caso)
    {
        using var fabrica = new FabricaApi(llm: LlmGenerico(), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync(id, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.True(fabrica.Llm.Llamadas >= 1, caso);
        var bloque = GuionesLlm.BloqueDeResumen(fabrica.Llm.Solicitudes[0], asignatura);
        Assert.NotNull(bloque);
        Assert.All(PersonasAjenasA(id), persona => Assert.DoesNotContain(persona, bloque.Cuerpo, StringComparison.Ordinal));
        Assert.True(respuesta.UsoDatosEstudiante, caso);
        Assert.Contains(CitaNotas, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-172")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_172_Una_asignatura_del_catalogo_en_la_que_no_esta_matriculado_se_verifica_con_una_lectura_y_no_entrega_resumen()
    {
        using var fabrica = new FabricaApi(llm: LlmGenerico(), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo voy en Contabilidad?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1, fabrica.Api.Llamadas.Count(l => l == "ObtenerNotas:EST-001"));
        Assert.Equal(0, LecturasDeAsistencia(fabrica));
        Assert.All(fabrica.Llm.Solicitudes, s => Assert.Null(GuionesLlm.BloqueDeResumen(s, "Cálculo Integral")));
        Assert.False(respuesta.UsoDatosEstudiante);
        Assert.DoesNotContain(respuesta.Citas, c => c.StartsWith("Datos del estudiante", StringComparison.Ordinal));
        Assert.Contains(RecursoConsultado.Notas, Assert.Single(fabrica.Auditoria.Eventos).RecursosConsultados);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-172")]
    [Trait("SEG", "SEG-04")]
    public async Task ESC_172_Un_tercero_junto_a_una_asignatura_recibe_403_antes_de_leer_notas_o_asistencia()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new InvalidOperationException("llm")), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Puedo habilitar Bases de Datos de Andrés Martínez?");

        Afirmaciones.Negativa(respuesta, "estudiante");
        Assert.Equal(0, fabrica.Llm.Llamadas);
        Assert.Equal(0, LecturasDeNotas(fabrica));
        Assert.Equal(0, LecturasDeAsistencia(fabrica));
    }

    [Fact]
    [Trait("CA", "CA-06")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-172")]
    [Trait("RF", "COMP-03")]
    public async Task ESC_172_Una_asignatura_nombrada_con_otro_periodo_responde_COMP_03_sin_llm_y_sin_leer_datos()
    {
        using var fabrica = new FabricaApi(llm: ClienteLlmDoble.Falla(new InvalidOperationException("llm")), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cómo voy en Bases de Datos en el periodo 2025-1?");

        Afirmaciones.SinInformacionSinLlm(respuesta, fabrica);
        Assert.Equal(0, LecturasDeNotas(fabrica));
    }

    [Theory]
    [InlineData("estudiante", "EST-001", "¿Con qué nota puedo habilitar una materia?", "ESC-155d")]
    [InlineData("estudiante", "EST-001", "¿Qué dice el reglamento sobre el cálculo de la definitiva?", "ESC-155f")]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-155")]
    [Trait("RF", "COMP-07")]
    public async Task ESC_155_Los_genericos_indefinidos_y_los_nombres_parciales_no_reciben_resumen(string rol, string id, string pregunta, string caso)
    {
        using var fabrica = new FabricaApi(llm: GuionesLlm.SinHerramientas(_ => "Respuesta de prueba."), conocimientoReal: true);

        var respuesta = await fabrica.PreguntarAsync(rol, id, pregunta);

        Assert.True(respuesta.Estado == 200, caso);
        Assert.True(fabrica.Llm.Llamadas >= 1, caso);
        Assert.All(fabrica.Llm.Solicitudes, s => Assert.DoesNotContain("CAL-201", TextoEnviado(s), StringComparison.Ordinal));
        Assert.Equal(0, LecturasDeNotas(fabrica));
        Assert.Equal(0, LecturasDeAsistencia(fabrica));
        Assert.False(respuesta.UsoDatosEstudiante, caso);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-032")]
    [Trait("RN", "RN-10")]
    public async Task ESC_032_Habilitar_Bases_de_Datos_de_EST_002_responde_con_datos_y_cita_notas_y_asistencia()
    {
        var llm = GuionesLlm.SinHerramientas(s => $"{GuionesLlm.LineaDe(s, "Bases de Datos")}\n{GuionesLlm.LineaDe(s, "Cálculo Integral")}");
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-002", "¿Puedo habilitar Bases de Datos?");

        Assert.Equal(200, respuesta.Estado);
        var lineaBd = GuionesLlm.LineaDe(fabrica.Llm.Solicitudes[0], "Bases de Datos");
        Assert.Matches(@"(?<![\d.])9(?![\d.])", lineaBd);
        Assert.Matches(@"(?<![\d.])48(?![\d.])", lineaBd);
        Assert.Matches(@"(?<![\d.])2\.6", GuionesLlm.LineaDe(fabrica.Llm.Solicitudes[0], "Cálculo Integral"));
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains(CitaNotas, respuesta.Citas);
        Assert.Contains(CitaAsistencia, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-031")]
    [Trait("RN", "RN-10")]
    public async Task ESC_031_Habilitar_materias_de_EST_005_responde_con_Estadistica_y_Microeconomia_y_cita_notas()
    {
        var llm = GuionesLlm.SinHerramientas(s => $"{GuionesLlm.LineaDe(s, "Estadística")}\n{GuionesLlm.LineaDe(s, "Microeconomía")}");
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-005", "¿Qué materias puedo habilitar?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("2.3", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("1.8", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains(CitaNotas, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-027")]
    [Trait("RN", "RN-13")]
    public async Task ESC_027_Las_faltas_restantes_en_Bases_de_Datos_reciben_el_resumen_y_responden_con_5()
    {
        var llm = GuionesLlm.SinHerramientas(s => GuionesLlm.LineaDe(s, "Bases de Datos"));
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-001", "¿Cuántas faltas más puedo tener en Bases de Datos sin perderla?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Equal(1, fabrica.Llm.Llamadas);
        Assert.Matches(@"(?<![\d.])5(?![\d.])", respuesta.Respuesta);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains(CitaAsistencia, respuesta.Citas);
    }

    [Fact]
    [Trait("CA", "CA-14")]
    [Trait("ESC", "ESC-029")]
    [Trait("RN", "RN-12")]
    public async Task ESC_029_La_beca_por_rendimiento_de_EST_004_recibe_el_resumen_con_el_promedio_3_88()
    {
        var llm = GuionesLlm.SinHerramientas(_ => "No cumples el requisito: tu promedio del periodo es 3.88.");
        using var fabrica = new FabricaApi(llm: llm, conocimientoReal: true);

        var respuesta = await fabrica.PreguntarEstudianteAsync("EST-004", "¿Puedo aplicar a la beca por rendimiento?");

        Assert.Equal(200, respuesta.Estado);
        Assert.Contains("3.88", TextoEnviado(fabrica.Llm.Solicitudes[0]), StringComparison.Ordinal);
        Assert.StartsWith("Datos de Camilo Rojas (EST-004)", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Promedio ponderado del periodo: 3.88", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.Contains("Beca por rendimiento académico: no elegible", respuesta.Respuesta, StringComparison.Ordinal);
        Assert.True(respuesta.UsoDatosEstudiante);
        Assert.Contains(CitaNotas, respuesta.Citas);
    }
}
